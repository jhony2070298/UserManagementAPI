# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

ASP.NET Core minimal API (.NET 10, `net10.0`, nullable + implicit usings enabled) for CRUD on users. Single project, no solution file, no test project, no database — users live in an in-memory `ConcurrentDictionary<Guid, User>` and are lost on restart.

## Commands

```powershell
dotnet build
$env:Authentication__BearerToken = "some-dev-token"; dotnet run                       # http://localhost:5095
$env:Authentication__BearerToken = "some-dev-token"; dotnet run --launch-profile https # https://localhost:7022
```

- The app **throws at startup** if `Authentication:BearerToken` is not configured (env var `Authentication__BearerToken`, user secrets, or appsettings). It is intentionally not in `appsettings.json`.
- There are no automated tests. Manual verification is done with [UserManagementAPI.http](UserManagementAPI.http) (VS Code REST Client / Visual Studio): set `@token` to the same value as the env var. It covers 401, 400 validation, 404, CRUD happy paths, and the 500 handler.
- OpenAPI document is served at `/openapi/v1.json` only in Development.

## Architecture

Everything except middleware is in [Program.cs](Program.cs): service registration, the pipeline, endpoint mappings, the `ValidateUserRequest` helper, and the `User` / `UserDetails` / `UserRequest` records (top-level, end of file).

**Pipeline order matters** (in `Program.cs`):
1. `UseExceptionHandler()` → [ApiExceptionHandler](Middleware/ApiExceptionHandler.cs) (`IExceptionHandler`) logs and returns `500 { "error": "Internal server error." }`.
2. [BearerTokenAuthenticationMiddleware](Middleware/BearerTokenAuthenticationMiddleware.cs) — guards **every** route (no anonymous endpoints, including OpenAPI). Compares the token with `CryptographicOperations.FixedTimeEquals`; on failure returns `401 { "error": "Unauthorized." }` with `WWW-Authenticate: Bearer` and logs the 401 itself, because it short-circuits before the logging middleware.
3. [RequestResponseLoggingMiddleware](Middleware/RequestResponseLoggingMiddleware.cs) — logs method/path/status via `Response.OnStarting`.
4. `UseStatusCodePages()`, then `UseHttpsRedirection()`.

Conventions to keep consistent when adding endpoints:
- Routes under `/api/users` with `{id:guid}` constraints; each mapping gets `.WithName(...)` and `.WithSummary(...)`.
- Request bodies are nullable (`UserRequest?`) and validated manually in `ValidateUserRequest` (trim, required, length limits: name/department ≤ 100, email ≤ 254 + `EmailAddressAttribute`). Failures return `Results.ValidationProblem(errors)` keyed by property name — not DataAnnotations model binding.
- Updates use `TryUpdate` against the previously read value to stay safe under concurrency.
- Error bodies from middleware use the `{ "error": "..." }` shape; validation errors use ProblemDetails.
- `/api/diagnostics/exception` exists only in Development to exercise the 500 handler.
