# Monorepo

.NET 10 monorepo hosting independently deployable APIs built with Clean + Hexagonal architecture.
The first service is **Orders** (`src/services/orders`).

## Contents

1. [Prerequisites](#1-prerequisites)
2. [Repository layout](#2-repository-layout)
3. [Restore and build](#3-restore-and-build)
4. [Run the API](#4-run-the-api)
5. [Example requests](#5-example-requests)
6. [Tests](#6-tests)
7. [Configuration](#7-configuration)
8. [Container image](#8-container-image)
9. [CI/CD and independent deployment](#9-cicd-and-independent-deployment)
10. [Troubleshooting](#10-troubleshooting)

## 1. Prerequisites

| Tool | Version | Required for | Verify |
| --- | --- | --- | --- |
| .NET SDK | `10.0.401` or a later 10.0.4xx feature band (see `global.json`, `rollForward: latestFeature`) | Everything | `dotnet --version` |
| Docker Desktop (Linux containers) | Any recent version, daemon running | Aspire AppHost, integration tests, local Postgres, container images | `docker version` (must show a `Server` section) |
| HTTPS dev certificate | - | Aspire dashboard (`https://localhost:17170`) | `dotnet dev-certs https --check --trust` |
| IDE (optional) | Visual Studio 2026, Rider or VS Code + C# Dev Kit | Editing; `.slnx` solutions need a current IDE version | - |

The Aspire CLI is **not** required: the AppHost uses the Aspire NuGet packages (`AspireUseCliBundle=false`).

## 2. Repository layout

```text
global.json                  SDK pin + Microsoft.Testing.Platform runner
Monorepo.slnx                Solution (folders mirror directories)
Directory.Build.props        net10.0, nullable, analyzers, warnings as errors
Directory.Packages.props     Central package versions (the only place versions live)
src/
  aspire/
    Monorepo.AppHost/        Local orchestration only (Postgres + orders-api)
    Monorepo.ServiceDefaults/ OpenTelemetry, health checks, service discovery, resilience, ProblemDetails
  building-blocks/
    BuildingBlocks.Abstractions/  Result / Result<T> / Error (no packages)
  services/orders/
    Orders.Domain/           Order aggregate, OrderLine, Money, OrderId, OrderStatus, OrderErrors
    Orders.Application/      Ports (Driving/Driven), Features (PlaceOrder, GetOrderById), DI
    Orders.Infrastructure/   EF Core + Npgsql adapter (DbContext, configurations, repository), DI
    Orders.Api/              Minimal API endpoints, composition root (Program.cs), container settings
tests/
  Directory.Build.props      Test defaults (xunit.v3, Microsoft.Testing.Platform, coverage)
  services/orders/
    Orders.UnitTests/        Domain + use-case tests (in-memory fake repository)
    Orders.ArchitectureTests/ Dependency-rule tests (NetArchTest)
    Orders.IntegrationTests/ WebApplicationFactory + Testcontainers PostgreSQL
.github/workflows/           ci.yml (whole repo), orders.yml (orders only)
```

### Dependency rule

```text
Orders.Api -> Orders.Infrastructure -> Orders.Application -> Orders.Domain -> BuildingBlocks.Abstractions
```

- `Orders.Domain` has no package references and no project references other than `BuildingBlocks.Abstractions`.
- `Orders.Application` must not depend on Infrastructure, Api, EF Core or ASP.NET Core.
- `Orders.Infrastructure` must not depend on Api.
- A service never references another service's projects.

`Orders.ArchitectureTests` enforces these rules on every build in CI.

### Adding a feature or endpoint

1. **Domain** (if new behavior/invariants): add or extend types under `Orders.Domain/Orders/`, return `Result` with errors from `OrderErrors`.
2. **Driving port**: add `I<UseCase>` under `Orders.Application/Ports/Driving/`.
3. **Use case**: add `Orders.Application/Features/<UseCase>/` with the command/query, a validator and an `internal sealed` handler implementing the port. Register it in `Orders.Application/DependencyInjection.cs`.
4. **Driven port / adapter** (if new persistence needs): extend `Ports/Driven/` and implement it in `Orders.Infrastructure/Persistence/`.
5. **Endpoint**: add `Orders.Api/Endpoints/<UseCase>Endpoint.cs` with a `Map<UseCase>` extension, map `Result` failures with `error.ToProblem()`, and chain it in `Program.cs`.
6. **Tests**: unit tests for the domain/handler, an integration test for the HTTP contract.

## 3. Restore and build

Whole solution:

```bash
dotnet restore Monorepo.slnx
dotnet build Monorepo.slnx
```

Orders only (builds Api, Infrastructure, Application, Domain, ServiceDefaults, BuildingBlocks):

```bash
dotnet build src/services/orders/Orders.Api
```

The build treats warnings as errors; it must finish with 0 warnings.

## 4. Run the API

### Option A: Aspire AppHost (recommended)

Starts a PostgreSQL container (with a persistent data volume), creates the `ordersdb` database and runs `orders-api` wired to it.

```bash
dotnet run --project src/aspire/Monorepo.AppHost
```

- The console prints a **login URL** with a one-time token, for example
  `Login to the dashboard at https://localhost:17170/login?t=<token>`. Open that link (opening `https://localhost:17170` alone asks for the token).
- The dashboard lists resources, logs, traces and metrics. `orders-api` is reachable at `http://localhost:5080`.
- The Postgres password is generated on first run and stored in the AppHost user secrets.

### Option B: Orders API standalone against a local PostgreSQL

1. Start PostgreSQL:

   ```bash
   docker run -d --name orders-db -p 5432:5432 \
     -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=ordersdb \
     postgres:17-alpine
   ```

2. Provide the connection string `ConnectionStrings:ordersdb` (environment variable `ConnectionStrings__ordersdb`):

   ```bash
   # bash
   export ConnectionStrings__ordersdb="Host=localhost;Port=5432;Database=ordersdb;Username=postgres;Password=postgres"
   ```

   ```powershell
   # PowerShell
   $env:ConnectionStrings__ordersdb = "Host=localhost;Port=5432;Database=ordersdb;Username=postgres;Password=postgres"
   ```

   Alternatively store it in user secrets (not committed):

   ```bash
   dotnet user-secrets set "ConnectionStrings:ordersdb" "Host=localhost;Port=5432;Database=ordersdb;Username=postgres;Password=postgres" --project src/services/orders/Orders.Api
   ```

3. Run the API (launch profile `http`, environment `Development`, schema created on startup):

   ```bash
   dotnet run --project src/services/orders/Orders.Api --launch-profile http
   ```

4. Clean up: `docker rm -f orders-db`.

### URLs

| Path | Purpose | Availability |
| --- | --- | --- |
| `http://localhost:5080/orders` | Orders endpoints | Always |
| `http://localhost:5080/scalar` | Scalar API reference UI | Development only |
| `http://localhost:5080/openapi/v1.json` | OpenAPI document | Development only |
| `http://localhost:5080/health` | Readiness: all health checks | Always |
| `http://localhost:5080/alive` | Liveness: checks tagged `live` | Always |

## 5. Example requests

Place an order:

```bash
curl -i -X POST http://localhost:5080/orders \
  -H "Content-Type: application/json" \
  -d '{
        "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "currency": "EUR",
        "lines": [
          { "sku": "SKU-1", "quantity": 2, "unitPrice": 10.25 },
          { "sku": "SKU-2", "quantity": 1, "unitPrice": 4.50 }
        ]
      }'
```

Response `201 Created` with a `Location: http://localhost:5080/orders/{id}` header:

```json
{ "id": "01a12816-35e3-7f8d-830e-875e036bb678" }
```

Get the order:

```bash
curl -i http://localhost:5080/orders/01a12816-35e3-7f8d-830e-875e036bb678
```

Response `200 OK`:

```json
{
  "id": "01a12816-35e3-7f8d-830e-875e036bb678",
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Placed",
  "currency": "EUR",
  "total": 25.00,
  "placedAt": "2026-10-10T23:11:46.657956+00:00",
  "lines": [
    { "sku": "SKU-1", "quantity": 2, "unitPrice": 10.25, "lineTotal": 20.50 },
    { "sku": "SKU-2", "quantity": 1, "unitPrice": 4.50, "lineTotal": 4.50 }
  ]
}
```

Errors are returned as RFC 9457 problem details with an application `code`:

| Case | Status | Example `code` |
| --- | --- | --- |
| Invalid input (no lines, bad currency, quantity <= 0, ...) | `400 Bad Request` | `Orders.NoLines` |
| Unknown order id | `404 Not Found` | `Orders.NotFound` |

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation failed",
  "status": 400,
  "detail": "An order must contain at least one line.",
  "code": "Orders.NoLines",
  "traceId": "00-..."
}
```

## 6. Tests

Tests use xunit.v3 on **Microsoft.Testing.Platform** (configured in `global.json`), so `dotnet test` takes `--solution` or `--project`.

| Command | Scope | Needs Docker |
| --- | --- | --- |
| `dotnet test --solution Monorepo.slnx` | All tests | Yes |
| `dotnet test --project tests/services/orders/Orders.UnitTests` | Domain + use cases | No |
| `dotnet test --project tests/services/orders/Orders.ArchitectureTests` | Dependency rule | No |
| `dotnet test --project tests/services/orders/Orders.IntegrationTests` | HTTP + PostgreSQL (Testcontainers) | Yes |

Run a single test or a subset (wildcards allowed):

```bash
dotnet test --project tests/services/orders/Orders.UnitTests --filter-method "*PlaceOrder_WithValidCommand*"
dotnet test --project tests/services/orders/Orders.UnitTests --filter-class "Orders.UnitTests.Domain.MoneyTests"
```

Code coverage (Microsoft.Testing.Extensions.CodeCoverage is referenced by every test project):

```bash
dotnet test --project tests/services/orders/Orders.UnitTests --coverage --coverage-output-format cobertura
```

The report path is printed at the end of the run (under `TestResults/`, which is git-ignored).

Integration tests start a `postgres:17-alpine` container per test class; the first run pulls the image and takes longer.

## 7. Configuration

| Source | Notes |
| --- | --- |
| `src/services/orders/Orders.Api/appsettings.json` | Logging, `AllowedHosts` |
| `src/services/orders/Orders.Api/appsettings.Development.json` | Development logging overrides |
| `src/services/orders/Orders.Api/Properties/launchSettings.json` | `http` profile: `http://localhost:5080`, `ASPNETCORE_ENVIRONMENT=Development` |
| Environment variables | Override any key; use `__` for `:` (e.g. `ConnectionStrings__ordersdb`) |
| User secrets | Local-only secrets for Api and AppHost |

| Key | Purpose |
| --- | --- |
| `ConnectionStrings:ordersdb` | PostgreSQL connection string (required). Injected automatically by the AppHost. |
| `ASPNETCORE_ENVIRONMENT` | `Development` enables Scalar, `/openapi/v1.json` and schema creation on startup |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | When set, telemetry is exported via OTLP (set automatically by the AppHost) |

**Database schema.** The schema (`orders` schema, tables `orders` and `order_lines`) is created with `EnsureCreated` only in `Development` and in integration tests. In any other environment the schema must already exist. EF Core migrations are a planned follow-up.

## 8. Container image

The API is containerized with the .NET SDK (no Dockerfile). Settings live in `Orders.Api.csproj`: repository `orders-api`, base image `mcr.microsoft.com/dotnet/aspnet:10.0`, default tag `latest`. The container listens on port `8080`.

Build into the local Docker daemon:

```bash
dotnet publish src/services/orders/Orders.Api -c Release -t:PublishContainer
```

Build to a tarball (no daemon or registry needed) and load it later:

```bash
dotnet publish src/services/orders/Orders.Api -c Release -t:PublishContainer -p:ContainerArchiveOutputPath=./artifacts/orders.tar.gz
docker load -i ./artifacts/orders.tar.gz
```

Run it next to PostgreSQL on a shared network:

```bash
docker network create orders-net
docker run -d --name orders-db --network orders-net \
  -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=ordersdb postgres:17-alpine
docker run -d --name orders-api --network orders-net -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e "ConnectionStrings__ordersdb=Host=orders-db;Port=5432;Database=ordersdb;Username=postgres;Password=postgres" \
  orders-api:latest
curl http://localhost:8080/health
```

`ASPNETCORE_ENVIRONMENT=Development` is used here only so the schema is created (see [Configuration](#7-configuration)). Clean up with `docker rm -f orders-api orders-db && docker network rm orders-net`.

## 9. CI/CD and independent deployment

| Workflow | Trigger | Does |
| --- | --- | --- |
| `.github/workflows/ci.yml` | Every pull request; push to `main` | Restore, build and test the whole `Monorepo.slnx` |
| `.github/workflows/orders.yml` | PR / push to `main` touching orders paths; tags `orders/v*` | Build and test orders only; on push publishes the container to GHCR |

`orders.yml` path filter: `src/services/orders/**`, `tests/services/orders/**`, `src/building-blocks/**`, `src/aspire/Monorepo.ServiceDefaults/**`, `Directory.*.props`, `Directory.Build.targets`, `global.json`, `.github/workflows/orders.yml`. Changes elsewhere (for example another service) do not trigger it.

Image publishing (`ghcr.io/<owner>/orders-api`):

| Event | Image tags |
| --- | --- |
| Push to `main` | `main`, `sha-<7-char sha>` |
| Tag `orders/v1.4.0` | `1.4.0`, `latest` |
| Tag `orders/v1.5.0-rc.1` | `1.5.0-rc.1` (pre-releases do not move `latest`) |

Release the orders API:

```bash
git tag orders/v1.0.0
git push origin orders/v1.0.0
```

Each service is versioned by its own tag prefix (`<service>/vX.Y.Z`), so services release independently.

### Checklist: adding a new API

- [ ] Create `src/services/<name>/<Name>.Domain|Application|Infrastructure|Api` mirroring orders (Domain references only `BuildingBlocks.Abstractions`).
- [ ] Create `tests/services/<name>/<Name>.UnitTests|ArchitectureTests|IntegrationTests`.
- [ ] Add all projects to `Monorepo.slnx` under `/src/services/<name>/` and `/tests/services/<name>/`.
- [ ] Add any new package versions to `Directory.Packages.props` only (no `Version` in `.csproj`).
- [ ] Set `ContainerRepository` in the Api `.csproj`.
- [ ] Register the API (and its database) in `src/aspire/Monorepo.AppHost/AppHost.cs`.
- [ ] Copy `.github/workflows/orders.yml` to `<name>.yml`, replace paths, project paths, `IMAGE_NAME` and the tag prefix `<name>/v*`.
- [ ] Do not reference another service's projects; share only through `src/building-blocks`.

## 10. Troubleshooting

| Symptom | Fix |
| --- | --- |
| `failed to connect to the docker API` / Testcontainers cannot find Docker | Start Docker Desktop and wait until `docker version` shows a `Server` section. Use Linux containers. |
| `A compatible .NET SDK was not found` / version mismatch | Install SDK `10.0.401` or later 10.0.4xx (`dotnet --list-sdks`). `global.json` allows newer feature bands only. |
| `Connection string 'ordersdb' is not configured.` | Set `ConnectionStrings__ordersdb` or use the AppHost (see [Run the API](#4-run-the-api)). |
| `relation "orders.orders" does not exist` | The schema is only created in `Development`. Run with `ASPNETCORE_ENVIRONMENT=Development` (migrations are a follow-up). |
| `address already in use` on 5080, 5432, 8080 or 17170 | Stop the process/container using the port (`docker ps`, `netstat -ano`), or change the port in `launchSettings.json` / `-p` mapping. |
| Browser certificate warning on the Aspire dashboard | `dotnet dev-certs https --trust` |
| Aspire Postgres fails to authenticate after resetting user secrets | Delete the old data volume (`docker volume ls`, `docker volume rm <name>`); the password is stored in AppHost user secrets. |
| Build fails on a new analyzer warning | Warnings are errors by design. Fix the code, or suppress narrowly with a justification. |
