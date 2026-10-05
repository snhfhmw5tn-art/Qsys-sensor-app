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

## Configure IIS once

1. In IIS Manager, create a site (or application) and set its physical path to the publish directory.
2. Choose an unused binding, such as `http://localhost:8085`; avoid a wildcard host binding.
3. Set the application pool's **.NET CLR Version** to **No Managed Code**.
4. Grant the site's application-pool identity read/execute access to the publish directory. Grant write access only to directories the app actually needs to modify.
5. Browse to the configured binding and confirm the app loads.

## Publish from Visual Studio

Right-click `Qsys.SensorApp.Web` (the server project), choose **Publish**, and select the `LocalIIS` profile. The profile uses folder deployment; it doesn't create an IIS site or configure bindings automatically. To change the output folder, set the MSBuild property `LocalIisPublishPath` when publishing.
