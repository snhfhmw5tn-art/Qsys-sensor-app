# Architecture overview

## Dependency rules

The Core project contains domain rules and value types and has no project dependencies. Application coordinates use cases and depends on Core. Infrastructure implements application-facing adapters and depends on Application and Core. The server-side Web and API entry points depend on Application. The WebAssembly client references Shared for browser-safe contracts.

## Runtime shape

The Web project hosts the Blazor Web App and its WebAssembly assets. The API is an independent HTTP service. Both can be run separately during development. API health is available at `/health` and `/api/health`.

## Milestone boundaries

Milestone 1 provides the runnable foundation and navigable product workspaces. Geometry and domain capabilities are introduced in later milestones with focused tests. UI pages must accurately communicate whether a capability is available and must not imply that unimplemented sensor or navigation work is active.
