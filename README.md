# OVCMOVE Service

Backend API for OVC Move, built with .NET 10 and Clean Architecture.

## Tech Stack

- .NET 10 ASP.NET Core Web API
- Clean Architecture
- MediatR 12.x
- AutoMapper 14.x
- SQL Server with Dapper
- Swagger / OpenAPI via Swashbuckle
- xUnit architecture tests

## Solution Structure

```text
src/
  OVCMOVE.Api             HTTP API, contracts, controllers, filters, middleware, host configuration
  OVCMOVE.Application     Use cases, MediatR handlers, DTOs, options, abstractions, behaviors
  OVCMOVE.Domain          Entities, value objects, domain events, domain services, domain rules
  OVCMOVE.Infrastructure  SQL Server, Dapper repositories, persistence, caching, external services
test/
  OVCMOVE.ArchitectureTests
docs/
  architecture/
  api/
  deployment/
  operations/
  decisions/
```

Dependency direction:

```text
Api -> Application -> Domain
Api -> Infrastructure -> Application -> Domain
```

`Domain` stays independent. `Application` owns interfaces. `Infrastructure` implements those interfaces.

## Local Configuration

Local `.env` is a good fit for this project if it is used only on developer machines and never committed.

1. Replace `{your_password}` in `.env` with the local/development database password.
2. Run the API with the `Local` launch profile.

The API loads `.env` only when `ASPNETCORE_ENVIRONMENT=Local`. In Production, Docker should pass environment variables into the container. The same `OVCMOVE_` keys work in both environments and override values from `appsettings.Local.json` / `appsettings.Production.json`.

Configuration keys:

```text
OVCMOVE_DbConfig__SQLServer__ConnectionString
OVCMOVE_ExternalServicesConfig__EmailService__Email
OVCMOVE_ExternalServicesConfig__EmailService__Password
```

Production Docker example:

```bash
docker run -p 8080:8080 \
  -e OVCMOVE_DbConfig__SQLServer__ConnectionString="{production_connection_string}" \
  -e OVCMOVE_ExternalServicesConfig__EmailService__Email="{production_email}" \
  -e OVCMOVE_ExternalServicesConfig__EmailService__Password="{production_email_password}" \
  ovc-move-service
```

## Run

```bash
dotnet restore
dotnet run --project src/OVCMOVE.Api --launch-profile Local
```

Swagger opens at:

```text
https://localhost:7093/swagger
```

Health/info sample:

```text
GET /api/system/info
```

## Test

```bash
dotnet test OVCMOVE.slnx
```

## Notes

- Docker and CI/CD files are intentionally not implemented yet.
- `.env` is ignored by git; commit only `.env.example`.
- AutoMapper 14.x is kept to stay before the newer licensed line. NuGet currently reports a security advisory for AutoMapper 13/14; the team should decide whether to move to AutoMapper 15+ with license handling or accept/suppress the advisory with a documented risk decision.
