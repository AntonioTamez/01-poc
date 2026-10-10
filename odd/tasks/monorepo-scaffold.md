# Feature: monorepo-scaffold

## Objective
Scaffold a .NET 10 monorepo with the first API (`orders`) built with Clean + Hexagonal architecture, ready for independent per-API deployment.

## Problem / Why
Empty repo. Need a reproducible base (shared config, CPM, CI) where each API builds, tests, ships and versions independently.

## Scope
- Root config: global.json (SDK 10.0.401), Monorepo.slnx, Directory.Build.props/targets, Directory.Packages.props (CPM), .editorconfig, .gitignore, .gitattributes, nuget.config, README.md.
- Aspire AppHost + ServiceDefaults (local dev only).
- BuildingBlocks.Abstractions (minimal, technical only).
- Orders: Domain, Application, Infrastructure, Api + UnitTests, IntegrationTests, ArchitectureTests.
- GitHub Actions: ci.yml (PR validation) + orders.yml (path-filtered, container publish, tag `orders/vX.Y.Z`).

## Constraints
- No API references another API's projects.
- Domain has zero package dependencies.
- Artifacts in English. Conventional commits, no AI attribution.

## Delivery strategy
`ask-on-risk`. Forecast > 400 authored lines (scaffold mostly boilerplate). Single feature branch `feat/monorepo-scaffold`.

## Tasks
- [x] T1 Root config files + solution — route: delegated (writer trigger: 2+ non-trivial files)
- [x] T2 Aspire AppHost + ServiceDefaults + BuildingBlocks.Abstractions — route: delegated
- [x] T3 Orders Domain + Application (ports, PlaceOrder, GetOrderById) + UnitTests — route: delegated
- [ ] T4 Orders Infrastructure (EF Core + Postgres) + Api (Minimal API, OpenAPI, Scalar) + Integration/Architecture tests — route: delegated
- [ ] T5 CI workflows (ci.yml, orders.yml) + README — route: delegated

## Acceptance criteria / Checks
- `dotnet build Monorepo.slnx` with 0 warnings.
- `dotnet test` green (unit + architecture; integration requires Docker).
- `dotnet publish src/services/orders/Orders.Api -t:PublishContainer` config valid.

## Progress / Evidence
- T1: commit `a33e1bf` — `dotnet build Monorepo.slnx` 0 errors, 0 warnings.
- T2: commit `fe3b5dc` — build 0 errors, 0 warnings (ASPIRE010 suppressed: CLI bundle not required; CA1716 suppressed on `Error`).
- T3: build 0/0; `dotnet test --project tests/services/orders/Orders.UnitTests` 22/22 passed.

## Next step
T4.
