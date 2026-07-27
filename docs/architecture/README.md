# Backend architecture

This service uses a pragmatic Clean Architecture. It intentionally does not
use DDD. Domain entities are data structures only; business rules belong to
Application use cases.

## Dependency rule

```text
HTTP request
    |
    v
API  --------->  Application  --------->  Domain
 |                    ^
 |                    |
 +---- composition ---+---- Infrastructure
```

Allowed project references:

| Project | May reference |
|---|---|
| `OVCMOVE.Domain` | no other OVCMOVE project |
| `OVCMOVE.Application` | `OVCMOVE.Domain` |
| `OVCMOVE.Infrastructure` | `OVCMOVE.Application`, `OVCMOVE.Domain` |
| `OVCMOVE.2026.Plugin` | `OVCMOVE.Application` |
| `OVCMOVE.Api` | `OVCMOVE.Application`, `OVCMOVE.Infrastructure` |

The API references Infrastructure only as the composition root that registers
and configures implementations. Controllers must communicate through
Application commands and queries, not Infrastructure services.

Architecture tests in `test/OVCMOVE.Test.Application/ArchitectureTests.cs`
protect these directions.

## Layer responsibilities

### Domain

Domain contains stable business data:

- entities and their fields;
- enums or constants that form the shared business vocabulary.

In this codebase, an entity must not validate, calculate, publish events, call a
repository, or mutate another entity. Defaults that express a use-case decision,
such as a new race being `draft`, are assigned by Application.

### Application

Application contains use cases:

- one command or query and one handler per operation;
- validation and business decisions;
- transaction orchestration;
- repository and external-service interfaces;
- use-case result models;
- explicit conversion from input to data-only entities.

A handler should read as a short workflow. When entity construction or
relationship synchronization obscures that workflow, use a named factory or
processor inside the same feature. These helpers are not Domain services.

Commands that write audited data inherit `AuditedRequest`. The MediatR
`AuditActorBehavior` supplies only the authenticated actor; the use case owns
the single timestamp used for all records in that operation. Do not copy
database audit fields into API request contracts.

Application exceptions describe expected failures:

- `ApplicationValidationException` -> invalid input;
- `ApplicationNotFoundException` -> missing resource;
- `ApplicationConflictException` -> invalid current state or duplicate data;
- `UnauthorizedAccessException` -> invalid authentication/authorization.

Do not catch an exception only to log and throw it unchanged. The API exception
middleware logs unexpected failures once.

### Infrastructure

Infrastructure implements Application ports:

- Dapper repositories and SQL;
- transaction and database connection management;
- JWT, password hashing, email, blob storage and Google authentication;
- background jobs;
- configuration classes.

A repository declares only the dependencies it uses. Avoid a generic base
repository that hides dependencies. SQL paging and filtering must happen in the
database rather than after loading an entire table.

Infrastructure failures are allowed to propagate. Do not convert configuration,
database or third-party outages into validation errors.

Unique-index violations are the exception: the shared database executor
translates SQL Server duplicate-key errors into `ApplicationConflictException`
so concurrent duplicate requests produce HTTP 409 rather than HTTP 500.

### API

API owns HTTP concerns:

- routes, authorization attributes and status codes;
- request/response contracts;
- multipart files and secure cookies;
- explicit contract-to-command and result-to-response mapping;
- the global exception-to-HTTP boundary.

Controllers should only:

1. read HTTP input;
2. map it to a command/query;
3. call MediatR;
4. map the result and return an HTTP response.

Controllers must not contain business rules, database calls, entity creation or
catch-all exception handling. API contracts must not be reused as Application
models.

## Standard feature structure

Use a vertical slice so all files for one operation are easy to find:

```text
Features/
  Races/
    Command/
      CreateRace/
        CreateRaceCommand.cs
        CreateRaceCommandHandler.cs
        CreateRaceFactory.cs       # only when mapping/validation is substantial
      PatchRace/
        PatchRaceCommand.cs
        PatchRaceCommandHandler.cs
        RacePatchMapper.cs
        BoothPatchProcessor.cs
    Query/
      GetAllRaces/
        GetAllRacesQuery.cs
        GetAllRacesQueryHandler.cs
```

Do not create one-file wrappers for trivial expressions. Split a file when it
has a second reason to change, for example:

- the handler orchestrates while a factory constructs entities;
- the main operation coordinates several independently changing relationships;
- formatting an email obscures the use-case flow.

Concrete helpers used by only one feature remain in that feature. Create an
Application interface only when crossing into Infrastructure or when there are
genuinely interchangeable implementations.

## Request flows

### Command

```text
API request contract
 -> explicit API mapping
 -> Application command
 -> handler validates and decides
 -> Application repository/service port
 -> Infrastructure implementation
 -> database or external system
 -> Application result
 -> explicit API response mapping
```

For an operation that writes several tables, the handler starts one unit of
work, commits after every required write succeeds, and rolls back on failure.
External side effects that cannot participate in that transaction need an
explicit policy. For example, organizer email is best-effort after the database
commit.

This database intentionally has no foreign keys. A use case that writes
relationships must therefore batch-check every referenced ID before starting
the transaction. Never rely only on a UI lookup or an earlier HTTP request for
referential integrity.

Many-to-many data uses relationship tables (`RaceTeam`, `RaceOrganizer`,
`BoothOrganizer`), never comma-separated IDs in an entity column. Read queries
return structured rows; only the API mapping may serialize them to preserve an
existing response shape.

For an existing database, run `sql/002_MigrateBoothOrganizer.sql` before
deploying code that reads `BoothOrganizer`. It copies valid legacy IDs without
resetting data and is safe to rerun. The old column is retained for one rollback
window and can be removed in a later deployment after the new version is stable.

### Query

Queries may return Application read models instead of Domain entities. This is
an intentional CQRS optimization: Infrastructure still implements the
Application-owned interface, and Application does not depend on Dapper.

### Error

```text
Application/Infrastructure exception
 -> GlobalExceptionMiddleware
 -> correct HTTP status
 -> one API response envelope
```

Never return HTTP 200 for a failed request.

## Review checklist

Before merging a backend feature, verify:

- dependencies point inward according to the table above;
- Domain entities contain fields only;
- API and Infrastructure types do not leak into Application;
- controller actions contain no business logic;
- mapping is explicit at the API boundary;
- handler code reads as one use-case workflow;
- transactions cover all required related writes;
- relationship IDs are validated in Application when the schema has no foreign keys;
- cancellation tokens reach database and external calls;
- paging/filtering runs in SQL;
- expected failures use a typed Application exception;
- passwords are hashed and secrets/tokens are not logged;
- new public or non-obvious methods have a short XML summary explaining intent;
- names use `Query`, not `Querry`, and `Persistence`, not `Persistance`;
- build has no warnings and relevant tests pass.

Run:

```bash
dotnet restore OVCMOVE.slnx
dotnet build OVCMOVE.slnx --no-restore
dotnet test OVCMOVE.slnx --no-build --no-restore
dotnet list OVCMOVE.slnx package --vulnerable --include-transitive
```
