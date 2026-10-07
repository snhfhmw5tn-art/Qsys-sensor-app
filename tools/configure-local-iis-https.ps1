[CmdletBinding()]
param(
    [string]$SiteName = "Sensor",
    [int]$HttpsPort = 443,
    [string]$FirewallRuleName = "Qsys Sensor IIS HTTPS LAN (TCP 443)",
    [string]$ServerDnsName = "SEQSYSLT250409A.qsys.se"
)

$ErrorActionPreference = "Stop"
$scriptDirectory = $PSScriptRoot
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $scriptDirectory "..")).TrimEnd('\')
$artifactDirectory = Join-Path $repositoryRoot "artifacts\iis-https"
$rootCertificatePath = Join-Path $artifactDirectory "QsysSensorLocalRootCA.cer"
$statusPath = Join-Path $artifactDirectory "iis-https-config-status.json"
$errorPath = Join-Path $artifactDirectory "iis-https-config-error.txt"

trap {
    New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
    [IO.File]::WriteAllText($errorPath, ($_ | Out-String) + [Environment]::NewLine + $_.ScriptStackTrace)
    exit 1
}

if (-not [Environment]::Is64BitProcess) { throw "Run this script in 64-bit Windows PowerShell." }
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run this script from an elevated PowerShell window. It creates a local CA, trusts it on this PC, binds HTTPS in IIS, and adds a LAN-scoped firewall rule."
}

Import-Module WebAdministration
Import-Module NetSecurity
Import-Module PKI

$site = Get-Website -Name $SiteName -ErrorAction Stop
$machineName = $env:COMPUTERNAME
$ServerDnsName = $ServerDnsName.Trim().TrimEnd('.')
if ([string]::IsNullOrWhiteSpace($ServerDnsName) -or $ServerDnsName.Contains('/')) {
    throw "ServerDnsName must be a valid DNS host name."
}
$lanAddress = Get-NetIPAddress -AddressFamily IPv4 -AddressState Preferred | Where-Object {
    $_.IPAddress -notlike "127.*" -and $_.PrefixLength -le 30 -and
    (Get-NetConnectionProfile -InterfaceIndex $_.InterfaceIndex -ErrorAction SilentlyContinue)
} | Select-Object -First 1
if (-not $lanAddress) { throw "No active LAN IPv4 address was found." }
$networkProfile = Get-NetConnectionProfile -InterfaceIndex $lanAddress.InterfaceIndex -ErrorAction Stop
$firewallProfile = switch ($networkProfile.NetworkCategory.ToString()) {
    "Public" { "Public"; break }
    "Private" { "Private"; break }
    "DomainAuthenticated" { "Domain"; break }
    default { throw "Unsupported network category '$($networkProfile.NetworkCategory)'." }
}

$addressBytes = $lanAddress.IPAddress.Split('.') | ForEach-Object { [int]$_ }
$remainingBits = [int]$lanAddress.PrefixLength
$networkBytes = for ($index = 0; $index -lt 4; $index++) {
    $bitsInByte = [Math]::Min(8, [Math]::Max(0, $remainingBits))
    if ($bitsInByte -eq 0) { 0 }
    else {
        $byteMask = (0xFF -shl (8 - $bitsInByte)) -band 0xFF
        $addressBytes[$index] -band $byteMask
    }
    $remainingBits -= $bitsInByte
}
$networkBytes = $networkBytes | ForEach-Object { [string]$_ }
$remoteSubnet = "{0}/{1}" -f ($networkBytes -join '.'), $lanAddress.PrefixLength

# Keep the FQDN usable on the IIS computer even when corporate DNS has no A record yet.
# Other devices still need the equivalent record in their shared LAN DNS.
$hostsPath = Join-Path $env:windir "System32\drivers\etc\hosts"
$hostsMappings = foreach ($line in [IO.File]::ReadAllLines($hostsPath)) {
    $hostFields = ($line -split '#', 2)[0].Trim() -split '\s+'
    if ($hostFields.Count -gt 1 -and $hostFields[1..($hostFields.Count - 1)] -contains $ServerDnsName) {
        $hostFields[0]
    }
}
if (@($hostsMappings).Count -gt 0 -and @($hostsMappings | Where-Object { $_ -ne $lanAddress.IPAddress }).Count -gt 0) {
    throw "The hosts file already maps '$ServerDnsName' to a different address. Resolve that entry before continuing."
}
if (@($hostsMappings).Count -eq 0) {
    $hostsEntry = "{0}`t{1} # Qsys Sensor local IIS" -f $lanAddress.IPAddress, $ServerDnsName
    [IO.File]::AppendAllText($hostsPath, [Environment]::NewLine + $hostsEntry + [Environment]::NewLine, [Text.Encoding]::ASCII)
}
Clear-DnsClientCache

$rootSubject = "CN=Qsys Sensor Local Development Root CA"
$rootCa = Get-ChildItem Cert:\LocalMachine\My | Where-Object {
    $_.Subject -eq $rootSubject -and $_.HasPrivateKey -and $_.NotAfter -gt (Get-Date)
} | Sort-Object NotAfter -Descending | Select-Object -First 1
if (-not $rootCa) {
    $rootCa = New-SelfSignedCertificate -Type Custom -Subject $rootSubject `
        -KeyAlgorithm RSA -KeyLength 3072 -HashAlgorithm SHA256 `
        -KeyUsage DigitalSignature,CertSign,CRLSign -KeyExportPolicy Exportable `
        -CertStoreLocation Cert:\LocalMachine\My `
        -TextExtension @("2.5.29.19={critical}{text}ca=TRUE") `
        -NotAfter (Get-Date).AddYears(10) -FriendlyName "Qsys Sensor Local Development Root CA"
}

$rootStore = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "LocalMachine")
$rootStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
try {
    $trustedRoot = @($rootStore.Certificates | Where-Object Thumbprint -eq $rootCa.Thumbprint)
    if ($trustedRoot.Count -eq 0) { $rootStore.Add($rootCa) }
}
finally { $rootStore.Close() }

$leafSubject = "CN=$ServerDnsName"
$serverCertificate = Get-ChildItem Cert:\LocalMachine\My | Where-Object {
    if ($_.Subject -ne $leafSubject -or -not $_.HasPrivateKey -or $_.NotAfter -le (Get-Date)) { return $false }
    $san = $_.Extensions | Where-Object { $_.Oid.Value -eq "2.5.29.17" } | Select-Object -First 1
    $sanText = if ($san) { $san.Format($false) } else { "" }
    $sanText -match [regex]::Escape($lanAddress.IPAddress) -and
        $sanText -match [regex]::Escape($machineName) -and
        $sanText -match [regex]::Escape($ServerDnsName) -and $sanText -match "localhost"
} | Sort-Object NotAfter -Descending | Select-Object -First 1

if (-not $serverCertificate) {
    $sanExtension = "2.5.29.17={text}DNS=$ServerDnsName&DNS=$machineName&DNS=localhost&IPAddress=$($lanAddress.IPAddress)&IPAddress=127.0.0.1"
    $serverCertificate = New-SelfSignedCertificate -Type Custom -Subject $leafSubject `
        -Signer $rootCa -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 `
        -KeyUsage DigitalSignature,KeyEncipherment -KeyExportPolicy Exportable `
        -CertStoreLocation Cert:\LocalMachine\My -TextExtension @(
            $sanExtension,
            "2.5.29.37={text}1.3.6.1.5.5.7.3.1"
        ) -NotAfter (Get-Date).AddYears(2) -FriendlyName "Qsys Sensor IIS HTTPS"
}

foreach ($bindingHost in @("localhost", $machineName, $ServerDnsName) | Select-Object -Unique) {
    $conflict = Get-Website | Where-Object { $_.Name -ne $SiteName } | ForEach-Object {
        $otherSite = $_
        $_.Bindings.Collection | Where-Object {
            $parts = ([string]$_.bindingInformation).Split(':', 3)
            $_.protocol -eq "https" -and $parts[1] -eq "$HttpsPort" -and
                $parts[2].Equals($bindingHost, [StringComparison]::OrdinalIgnoreCase)
        } | ForEach-Object { $otherSite.Name }
    } | Select-Object -First 1
    if ($conflict) {
        throw "HTTPS hostname '$bindingHost' on port $HttpsPort is already bound to IIS site '$conflict'. No existing site binding was changed."
    }

    $bindingInformation = "*:${HttpsPort}:$bindingHost"
    $desiredBinding = Get-WebBinding -Name $SiteName -Protocol https | Where-Object {
        ([string]$_.bindingInformation) -eq $bindingInformation
    } | Select-Object -First 1
    if (-not $desiredBinding) {
        New-WebBinding -Name $SiteName -Protocol https -IPAddress "*" -Port $HttpsPort -HostHeader $bindingHost -SslFlags 1
        $desiredBinding = Get-WebBinding -Name $SiteName -Protocol https | Where-Object {
            ([string]$_.bindingInformation) -eq $bindingInformation
        } | Select-Object -First 1
    }
    if (-not $desiredBinding) { throw "Could not create HTTPS binding '$bindingInformation' on IIS site '$SiteName'." }
    $desiredBinding.AddSslCertificate($serverCertificate.Thumbprint, "My")
}

$existingRule = Get-NetFirewallRule -DisplayName $FirewallRuleName -ErrorAction SilentlyContinue
if ($existingRule) { $existingRule | Remove-NetFirewallRule }
New-NetFirewallRule -DisplayName $FirewallRuleName -Direction Inbound -Action Allow `
    -Protocol TCP -LocalPort $HttpsPort -RemoteAddress $remoteSubnet -Profile $firewallProfile `
    -Description "Allow Qsys Sensor HTTPS from this computer's local IPv4 subnet only." | Out-Null

New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
Export-Certificate -Cert $rootCa -FilePath $rootCertificatePath -Force | Out-Null
if ((Get-WebAppPoolState -Name ([string]$site.applicationPool)).Value -ne "Started") {
    Start-WebAppPool -Name ([string]$site.applicationPool)
}
if ((Get-WebsiteState -Name $SiteName).Value -ne "Started") { Start-Website -Name $SiteName }

$result = [pscustomobject]@{
    Site = $SiteName
    State = (Get-WebsiteState -Name $SiteName).Value
    ApplicationPool = [string]$site.applicationPool
    ApplicationPoolState = (Get-WebAppPoolState -Name ([string]$site.applicationPool)).Value
    Bindings = @("https://localhost:${HttpsPort}/", "https://${machineName}:${HttpsPort}/", "https://${ServerDnsName}:${HttpsPort}/")
    ComputerName = $machineName
    ComputerIPv4 = $lanAddress.IPAddress
    NetworkProfile = $networkProfile.NetworkCategory.ToString()
    FirewallRemoteSubnet = $remoteSubnet
    FirewallRule = $FirewallRuleName
    CertificateThumbprint = $serverCertificate.Thumbprint
    CertificateExpires = $serverCertificate.NotAfter
    TrustedRootCertificate = $rootCertificatePath
    LocalUrl = "https://localhost/"
    LanUrl = "https://${ServerDnsName}/ (must resolve to $($lanAddress.IPAddress) on LAN)"
}
$result | ConvertTo-Json | Set-Content -LiteralPath $statusPath -Encoding UTF8
Remove-Item -LiteralPath $errorPath -Force -ErrorAction SilentlyContinue
$result
