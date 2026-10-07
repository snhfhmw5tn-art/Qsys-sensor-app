# Publish to local IIS

The web host includes a `LocalIIS` folder publish profile. It publishes the ASP.NET Core host and its Blazor WebAssembly assets in Release configuration. The default output is `artifacts/iis` at the repository root, which you can use as the physical path for a local IIS site.

## Requirements

- Windows with IIS installed and enabled.
- The .NET 10 Hosting Bundle installed on the IIS machine. It installs the ASP.NET Core Module and runtime needed by IIS.
- An IIS site or application whose physical path points to the publish folder.

Install IIS before the Hosting Bundle. If IIS is installed after the bundle, repair/re-run the bundle installer. See Microsoft's [IIS hosting guide](https://learn.microsoft.com/aspnet/core/host-and-deploy/iis/?view=aspnetcore-10.0) and [Hosting Bundle guide](https://learn.microsoft.com/aspnet/core/host-and-deploy/iis/hosting-bundle?view=aspnetcore-10.0).

## Publish from the command line

From the repository root:

```powershell
dotnet publish src/Qsys.SensorApp.Web/Qsys.SensorApp.Web/Qsys.SensorApp.Web.csproj `
  --configuration Release `
  --no-restore `
  /p:PublishProfile=LocalIIS
```

The profile writes to `artifacts/iis` and generates the `web.config` IIS needs. To publish directly to another IIS site's physical path, pass that path explicitly:

```powershell
dotnet publish src/Qsys.SensorApp.Web/Qsys.SensorApp.Web/Qsys.SensorApp.Web.csproj `
  --configuration Release `
  --no-restore `
  /p:PublishProfile=LocalIIS `
  /p:LocalIisPublishPath="C:\inetpub\wwwroot\Qsys.SensorApp"
```

The publishing account needs write access to the target. Existing files are not deleted by this profile. Use a dedicated application directory and point IIS to it; don't publish over another site's content.

## Configure a published IIS site

1. In IIS Manager, create a site (or application) and set its physical path to the publish directory.
2. Choose an unused binding, such as `http://localhost:8085`; avoid a wildcard host binding.
3. Set the application pool's **.NET CLR Version** to **No Managed Code**.
4. Grant the site's application-pool identity read/execute access to the publish directory. Grant write access only to directories the app actually needs to modify.
5. Browse to the configured binding and confirm the app loads.

## Local IIS HTTPS and Visual Studio debugging

For this development machine, run `tools/configure-local-iis-https.ps1` from an elevated, 64-bit Windows PowerShell session. The script adds SNI HTTPS bindings for `localhost`, the computer name, and `SEQSYSLT250409A.qsys.se` on port 443, keeps existing port 443 bindings intact, adds a local hosts-file entry for the FQDN on the IIS computer, starts the `Sensor` application pool, and adds an inbound firewall rule restricted to the active LAN subnet. It creates a local development root CA and exports only its public certificate to `artifacts/iis-https/QsysSensorLocalRootCA.cer`; the root CA private key stays on the IIS computer. Other devices still require the FQDN to be registered in the LAN DNS.

The tracked `IIS HTTPS` launch profile in `src/Qsys.SensorApp.Web/Qsys.SensorApp.Web/Properties/launchSettings.json` uses `https://SEQSYSLT250409A.qsys.se/` for Visual Studio and LAN testing. Select **IIS HTTPS** in Visual Studio's launch-profile dropdown. Make sure local DNS resolves the FQDN to the IIS computer's LAN IP. The existing global port 443 binding uses a different certificate, so the raw IP URL won't select the Sensor certificate; keep the FQDN in the URL to use SNI.

To use browser sensor APIs on another device, install the exported public root certificate as a trusted CA on that device, then browse to the computer-name URL. A self-managed development CA is for local testing; use a certificate issued by your organization's trusted CA for broader deployment.

IIS holds Debug output files open while the app pool is running. Stop the `SensorAppPool` before rebuilding in Visual Studio, then start it again after the build if Visual Studio doesn't do so automatically.

## Publish from Visual Studio

Right-click `Qsys.SensorApp.Web` (the server project), choose **Publish**, and select the `LocalIIS` profile. The profile uses folder deployment; it doesn't create an IIS site or configure bindings automatically. To change the output folder, set the MSBuild property `LocalIisPublishPath` when publishing.
