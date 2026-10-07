[CmdletBinding()]
param(
    [string]$SiteName = "Sensor",
    [int]$Port = 8085,
    [string]$FirewallRuleName = "Qsys Sensor IIS LAN (TCP 8085)",
    [string]$PublishPath
)

$ErrorActionPreference = "Stop"
$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
}
if ([string]::IsNullOrWhiteSpace($PublishPath)) {
    $PublishPath = Join-Path $scriptDirectory "..\artifacts\iis"
}
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $scriptDirectory ".." )).TrimEnd('\')
$statusPath = Join-Path $repositoryRoot "artifacts\iis-lan-config-status.json"
$errorPath = Join-Path $repositoryRoot "artifacts\iis-lan-config-error.txt"
trap {
    New-Item -ItemType Directory -Path (Split-Path $errorPath -Parent) -Force | Out-Null
    [IO.File]::WriteAllText($errorPath, ($_ | Out-String) + [Environment]::NewLine + $_.ScriptStackTrace)
    exit 1
}

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run this script from an elevated PowerShell window. It changes an IIS binding and adds a LAN-scoped inbound firewall rule."
}

Import-Module WebAdministration
Import-Module NetSecurity

$site = Get-Website -Name $SiteName -ErrorAction Stop
$physicalPath = [Environment]::ExpandEnvironmentVariables([string]$site.PhysicalPath)
$poolName = [string]$site.applicationPool
if ([string]::IsNullOrWhiteSpace($physicalPath) -or [string]::IsNullOrWhiteSpace($poolName)) {
    throw "IIS site '$SiteName' must have a physical path and application pool configured."
}
$physicalPath = [IO.Path]::GetFullPath($physicalPath).TrimEnd('\')
$appPoolIdentity = "IIS AppPool\$poolName"
$webProjectPath = [IO.Path]::GetFullPath((Join-Path $repositoryRoot "src\Qsys.SensorApp.Web\Qsys.SensorApp.Web")).TrimEnd('\')
$clientProjectPath = [IO.Path]::GetFullPath((Join-Path $repositoryRoot "src\Qsys.SensorApp.Web\Qsys.SensorApp.Web.Client")).TrimEnd('\')
$isDevelopmentSite = $physicalPath.Equals($webProjectPath, [StringComparison]::OrdinalIgnoreCase)
if (-not $isDevelopmentSite -and ($physicalPath.Equals($repositoryRoot, [StringComparison]::OrdinalIgnoreCase) -or $physicalPath.StartsWith($repositoryRoot + '\', [StringComparison]::OrdinalIgnoreCase))) {
    throw "Refusing to deploy Release files into the repository '$repositoryRoot'. Point IIS at a dedicated publish directory, or use the project's IIS Debug profile."
}

$publishRoot = $null
if (-not $isDevelopmentSite) {
    $publishRoot = (Resolve-Path -LiteralPath $PublishPath -ErrorAction Stop).Path
    if (-not (Test-Path -LiteralPath (Join-Path $publishRoot "web.config"))) {
        throw "No IIS publish output with web.config found at '$publishRoot'. Publish the LocalIIS profile first."
    }
    $encryptedFile = Get-ChildItem -LiteralPath $publishRoot -File -Recurse -Force | Where-Object {
        $_.Attributes -band [IO.FileAttributes]::Encrypted
    } | Select-Object -First 1
    if ($encryptedFile) { throw "Publish output contains an EFS-encrypted file ('$($encryptedFile.FullName)'). Publish it to an unencrypted folder first." }
    New-Item -ItemType Directory -Path $physicalPath -Force | Out-Null
    & icacls.exe $physicalPath /grant "${appPoolIdentity}:(OI)(CI)(RX)" /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not grant read/execute access to '$appPoolIdentity' on '$physicalPath'." }
}

# IIS debug hosting reads the app and Blazor's generated static web assets directly from the repo.
# Grant traverse-only on the parents and inherited read/execute only to the two web projects.
$parentPath = Split-Path $physicalPath -Parent
while ($parentPath -and [IO.Path]::GetFullPath($parentPath).TrimEnd('\') -ne [IO.Path]::GetPathRoot($parentPath).TrimEnd('\')) {
    & icacls.exe $parentPath /grant "${appPoolIdentity}:(X)" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not grant directory traversal to '$appPoolIdentity' on '$parentPath'." }
    $parentPath = Split-Path $parentPath -Parent
}
if ($isDevelopmentSite) {
    foreach ($projectPath in @($webProjectPath, $clientProjectPath)) {
        & icacls.exe $projectPath /grant "${appPoolIdentity}:(OI)(CI)(RX)" | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Could not grant read/execute access to '$appPoolIdentity' on '$projectPath'." }
    }
}

$bindings = @(Get-WebBinding -Name $SiteName -Protocol "http")
$conflict = Get-Website | Where-Object Name -ne $SiteName | ForEach-Object {
    $otherSite = $_
    $_.Bindings.Collection | Where-Object {
        $_.protocol -eq "http" -and ([string]$_.bindingInformation).Split(':', 3)[1] -eq "$Port" -and
        [string]::IsNullOrEmpty(([string]$_.bindingInformation).Split(':', 3)[2])
    } | ForEach-Object { $otherSite.Name }
} | Select-Object -First 1
if ($conflict) { throw "Port $Port already has a wildcard HTTP binding on IIS site '$conflict'. Resolve the binding conflict first." }

$ipBindings = Get-NetIPAddress -AddressFamily IPv4 -AddressState Preferred | Where-Object {
    $_.IPAddress -notlike "127.*" -and $_.PrefixLength -le 30
}
$lanAddress = $ipBindings | Where-Object {
    (Get-NetConnectionProfile -InterfaceIndex $_.InterfaceIndex -ErrorAction SilentlyContinue) -ne $null
} | Select-Object -First 1
if (-not $lanAddress) { throw "No active IPv4 network interface was found for the IIS LAN binding." }

$profile = Get-NetConnectionProfile -InterfaceIndex $lanAddress.InterfaceIndex -ErrorAction Stop
$firewallProfile = switch ($profile.NetworkCategory.ToString()) {
    "Public" { "Public"; break }
    "Private" { "Private"; break }
    "DomainAuthenticated" { "Domain"; break }
    default { throw "Unsupported network category '$($profile.NetworkCategory)' for the IIS firewall rule." }
}
$prefix = [int]$lanAddress.PrefixLength
$addressBytes = $lanAddress.IPAddress.Split('.') | ForEach-Object { [int]$_ }
$remainingBits = $prefix
$networkBytes = for ($index = 0; $index -lt 4; $index++) {
    $bitsInByte = [Math]::Min(8, [Math]::Max(0, $remainingBits))
    if ($bitsInByte -eq 0) {
        0
    }
    else {
        $byteMask = (0xFF -shl (8 - $bitsInByte)) -band 0xFF
        $addressBytes[$index] -band $byteMask
    }
    $remainingBits -= $bitsInByte
}
$networkBytes = $networkBytes | ForEach-Object { [string]$_ }
$remoteSubnet = "{0}/{1}" -f ($networkBytes -join '.'), $prefix

foreach ($binding in $bindings) {
    $parts = ([string]$binding.bindingInformation).Split(':', 3)
    if ($parts[1] -eq "$Port" -and $parts[2] -eq "localhost") {
        Remove-WebBinding -Name $SiteName -Protocol "http" -Port $Port -HostHeader "localhost" -IPAddress $parts[0]
    }
}

$desired = Get-WebBinding -Name $SiteName -Protocol "http" | Where-Object {
    ([string]$_.bindingInformation) -eq "*:${Port}:"
}
if (-not $desired) { New-WebBinding -Name $SiteName -Protocol "http" -IPAddress "*" -Port $Port -HostHeader "" }

$offlineFile = Join-Path $physicalPath "app_offline.htm"
if (-not $isDevelopmentSite) {
    if (Test-Path -LiteralPath $offlineFile) {
        throw "'$offlineFile' already exists. It was left untouched; inspect the site before deploying."
    }
    Set-Content -LiteralPath $offlineFile -Value "Updating $SiteName" -Encoding UTF8
    try {
        $publishRootPrefix = $publishRoot.TrimEnd('\') + '\'
        foreach ($sourceFile in Get-ChildItem -LiteralPath $publishRoot -File -Recurse -Force) {
            $relativePath = $sourceFile.FullName.Substring($publishRootPrefix.Length)
            $targetFile = Join-Path $physicalPath $relativePath
            $targetDirectory = Split-Path -Parent $targetFile
            New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
            if (Test-Path -LiteralPath $targetFile) { Remove-Item -LiteralPath $targetFile -Force }

            $inputStream = [IO.File]::OpenRead($sourceFile.FullName)
            try {
                $outputStream = [IO.File]::Open($targetFile, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
                try { $inputStream.CopyTo($outputStream) }
                finally { $outputStream.Dispose() }
            }
            finally { $inputStream.Dispose() }
            [IO.File]::SetLastWriteTimeUtc($targetFile, $sourceFile.LastWriteTimeUtc)
        }
    }
    finally {
        Remove-Item -LiteralPath $offlineFile -Force -ErrorAction SilentlyContinue
    }
}

$existingRule = Get-NetFirewallRule -DisplayName $FirewallRuleName -ErrorAction SilentlyContinue
if ($existingRule) {
    $existingRule | Remove-NetFirewallRule
}
New-NetFirewallRule -DisplayName $FirewallRuleName -Direction Inbound -Action Allow -Protocol TCP -LocalPort $Port -RemoteAddress $remoteSubnet -Profile $firewallProfile -Description "Allow the Qsys Sensor IIS site from this computer's local IPv4 subnet only." | Out-Null

if ((Get-WebAppPoolState -Name $poolName).Value -ne "Started") { Start-WebAppPool -Name $poolName }
if ((Get-WebsiteState -Name $SiteName).Value -ne "Started") { Start-Website -Name $SiteName }

$result = [pscustomobject]@{
    Site = $SiteName
    State = (Get-WebsiteState -Name $SiteName).Value
    ApplicationPool = $poolName
    ApplicationPoolState = (Get-WebAppPoolState -Name $poolName).Value
    PhysicalPath = $physicalPath
    Mode = $(if ($isDevelopmentSite) { "Visual Studio IIS debug" } else { "Published files" })
    PublishedFrom = $publishRoot
    Binding = "http://*:${Port}/"
    ComputerIPv4 = $lanAddress.IPAddress
    NetworkProfile = $profile.NetworkCategory.ToString()
    FirewallRemoteSubnet = $remoteSubnet
    FirewallRule = $FirewallRuleName
    DeviceUrl = "http://$($lanAddress.IPAddress):$Port/"
}
$result | ConvertTo-Json | Set-Content -LiteralPath $statusPath -Encoding UTF8
$result
