# Qsys Sensor App

Qsys Sensor App is a warehouse indoor-navigation platform under active development. This repository is being built in milestones, with the solution foundation delivered first and domain capabilities added in later increments.

## Current milestone

Milestone 1 establishes a runnable .NET 10 solution with a Blazor Web App using Interactive WebAssembly, a separate ASP.NET Core API, Clean Architecture project boundaries, an MSTest project, documentation, and continuous integration.

The drawing, sensor, navigation and settings pages are usable workspace shells. Sensor collection, map editing, positioning, and warehouse data are not implemented yet.

## Requirements

- .NET 10 SDK
- Git

## Build and run

```powershell
dotnet restore Qsys.SensorApp.sln
dotnet build Qsys.SensorApp.sln --no-restore
dotnet test Qsys.SensorApp.sln --no-build
dotnet run --project src/Qsys.SensorApp.Api
dotnet run --project src/Qsys.SensorApp.Web/Qsys.SensorApp.Web
```

The API exposes `/health` and `/api/health`. The web app is served by the ASP.NET Core host and loads its interactive UI from the WebAssembly client project.

## Architecture

```text
Web ───────────────> Application ──> Core
API ───────────────> Application ──> Core
API ───────────────> Infrastructure ──> Application + Core
Web.Client ────────> Shared
```

`Core` has no project references. `Application` depends on Core. `Infrastructure` depends on Application and Core. Web and API depend on Application. The `Web.Client` project is required by the Blazor Interactive WebAssembly hosting model; Shared contains types that must be used by both browser and server.

## Repository layout

- `src/` — application projects
- `tests/` — MSTest projects
- `docs/` — architecture and product documentation
- `samples/` — sample maps and datasets
- `tools/` — developer utilities
- `.github/workflows/` — build and test automation

## Development rules

- Keep project references aligned with the architecture above.
- Use nullable reference types and implicit usings.
- Manage NuGet versions centrally in `Directory.Packages.props`.
- Name MSTest methods `TestThat_<snake_case_description>`.
- Restore, build and test before committing changes.
- Do not merge placeholder implementations into production capabilities; represent not-yet-implemented capabilities honestly in the interface.
