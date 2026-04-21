# Copilot Cloud Agent Instructions

This is a **Clean Architecture Solution Template** for ASP.NET Core 10 — a reference implementation and NuGet template (`Clean.Architecture.Solution.Template`) that supports Angular, React, or API-only frontends with SQLite, SQL Server, or PostgreSQL.

---

## Repository Layout

```
src/
  Domain/           # Entities, ValueObjects, Events, Enums, Constants – no dependencies
  Application/      # CQRS commands/queries, behaviours, interfaces, DTOs – depends on Domain only
  Infrastructure/   # EF Core DbContext, Identity, interceptors – implements Application interfaces
  Web/              # ASP.NET Core Minimal API, Endpoints, DI wiring – entry point
  AppHost/          # .NET Aspire orchestration host
  ServiceDefaults/  # Shared Aspire service defaults (telemetry, health checks)
  Shared/           # Shared constants (Services class) used across projects
tests/
  Domain.UnitTests/
  Application.UnitTests/
  Application.FunctionalTests/   # Sends MediatR commands against a real SQLite DB via TestApp helpers
  Infrastructure.IntegrationTests/
  Web.AcceptanceTests/           # Playwright + Reqnroll BDD (only for Angular/React builds)
```

---

## Technology Stack

| Concern | Library |
|---|---|
| Framework | ASP.NET Core 10 / .NET 10 |
| Orchestration | .NET Aspire 13 |
| ORM | Entity Framework Core 10 |
| CQRS | MediatR 14 |
| Mapping | AutoMapper 16 |
| Validation | FluentValidation 12 |
| Auth | ASP.NET Core Identity (cookie or bearer, depending on template config) |
| API docs | Scalar + `Microsoft.AspNetCore.OpenApi` |
| Testing | NUnit 4, Shouldly, Moq, Respawn |
| Guard clauses | Ardalis.GuardClauses |

All package versions are centralised in `Directory.Packages.props` — **never specify versions in individual `.csproj` files**.

The solution targets **net10.0** (set in `Directory.Build.props`). `TreatWarningsAsErrors` is enabled globally.

---

## Architecture Rules

1. **Domain** has zero external dependencies. Entities extend `BaseAuditableEntity` (which extends `BaseEntity` with an `int Id`). Domain events are raised inside entity setters via `AddDomainEvent(...)`.
2. **Application** depends only on Domain. It defines `IApplicationDbContext` (the only EF abstraction used by handlers). Commands/queries are `record` types implementing `IRequest<T>`.
3. **Infrastructure** implements `IApplicationDbContext` via `ApplicationDbContext` (inherits `IdentityDbContext<ApplicationUser>`). EF Configurations live in `Infrastructure/Data/Configurations/`. Two interceptors run on every save: `AuditableEntityInterceptor` (sets Created/Modified stamps) and `DispatchDomainEventsInterceptor`.
4. **Web** depends on Application and Infrastructure for DI wiring only. Business logic must not live here.

---

## CQRS Conventions

- Each feature lives in a folder under `Application/<Feature>/Commands/<CommandName>/` or `Application/<Feature>/Queries/<QueryName>/`.
- The command/query `record` and its handler class live in the **same file**.
- Validators (`AbstractValidator<TCommand>`) live in a **separate file** in the same folder, named `<CommandName>Validator.cs`.
- AutoMapper `Profile` inner classes (`private class Mapping : Profile`) are defined inside the DTO they map into.
- Apply `[Authorize]` (the custom `CleanArchitecture.Application.Common.Security.AuthorizeAttribute`, not the ASP.NET one) to commands/queries that require authentication. Roles and policies are checked by `AuthorizationBehaviour`.

### MediatR Pipeline (in order)

1. `LoggingBehaviour` (pre-processor)
2. `UnhandledExceptionBehaviour`
3. `AuthorizationBehaviour`
4. `ValidationBehaviour` — throws `ValidationException` on failures
5. `PerformanceBehaviour`

---

## Endpoint Conventions

Endpoints are grouped with `IEndpointGroup`. Create a class in `src/Web/Endpoints/` implementing `IEndpointGroup` with a static `Map(RouteGroupBuilder)` method. The class name determines the route: `/api/{ClassName}`. Override the static `RoutePrefix` property to change it.

`WebApplicationExtensions.MapEndpoints()` auto-discovers all `IEndpointGroup` implementations via reflection at startup.

Handler methods are static, receive `ISender sender` and typed parameters, and return `TypedResults` (e.g. `Created<T>`, `NoContent`, `Ok<T>`, `Results<A, B>`). Decorate each handler with `[EndpointSummary]` and `[EndpointDescription]`.

---

## Database

Default database is **SQLite** (file-based, no Docker needed). SQL Server and PostgreSQL are supported via `#if` compile symbols (`UseSqlServer`, `UsePostgreSQL`). The active provider is wired in `Infrastructure/DependencyInjection.cs`.

The connection string key is `CleanArchitectureDb` (defined in `CleanArchitecture.Shared.Services.Database`).

In development, `InitialiseDatabaseAsync()` runs `EnsureDeleted` + `EnsureCreated` and seeds default data including an `administrator@localhost` / `Administrator1!` user with the `Administrator` role.

EF migrations are not used in the default SQLite setup — `EnsureCreated` is used instead.

---

## Testing

### Functional Tests (`Application.FunctionalTests`)

- Use `TestApp` static helpers: `SendAsync`, `RunAsDefaultUserAsync`, `RunAsAdministratorAsync`, `FindAsync<T>`, `AddAsync<T>`, `CountAsync<T>`.
- Tests extend `TestBase` which resets DB state in `[SetUp]` via Respawn.
- `WebApiFactory` replaces `IUser` with a mock so tests can simulate any user/role combination.
- Test classes should inherit `TestBase`; each test is an async `[Test]` method.

### Unit Tests (`Application.UnitTests`)

- Use Moq to mock `IApplicationDbContext` and other interfaces.
- Use Shouldly for assertions.

---

## Build, Test & Run

```bash
# Restore + build (Release)
dotnet restore
dotnet build --no-restore --configuration Release

# Run all tests
dotnet test --no-build --configuration Release

# Run the app (Aspire dashboard opens automatically)
dotnet run --project src/AppHost
```

The CI pipeline (`.github/workflows/build.yml`) runs `dotnet restore → dotnet build → dotnet test` on Ubuntu with NuGet caching.

---

## Global Usings

Each project has a `GlobalUsings.cs`. Common usings (`MediatR`, `AutoMapper`, `FluentValidation`, `Microsoft.EntityFrameworkCore`, etc.) are already declared there — do not add redundant usings in individual files.

---

## Naming & Style

- `TreatWarningsAsErrors` is **on**. Fix all warnings.
- Nullable reference types are **enabled** (`<Nullable>enable</Nullable>`). Use `?` annotations appropriately; avoid `null!` unless unavoidable.
- `ImplicitUsings` are enabled. Do not add usings already covered by global usings.
- Commands and queries are `record` types. Entities and handlers are `class` types.
- Use `init` accessors for DTO and command properties.
- Prefer `private readonly` fields over properties for injected dependencies in handlers.

---

## Common Errors & Workarounds

- **`NU1608` warning for Npgsql**: Suppressed globally in `Directory.Build.props` with `<WarningsNotAsErrors>NU1608</WarningsNotAsErrors>` when PostgreSQL is in use, because `Npgsql.EntityFrameworkCore.PostgreSQL` v10 is not yet released.
- **Template `#if` directives**: The source tree contains `#if (UsePostgreSQL)`, `#if (UseSqlServer)`, `#if (UseApiOnly)` etc. as dotnet template conditionals (not C# preprocessor). Do not remove them — the live repo defaults to SQLite, so the `#else` / default branch is always active.
- **`RelationalEventId.PendingModelChangesWarning`**: Suppressed in `AddDbContext` configuration to silence EF Core noise during development.
