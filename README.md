# User Registration and Notification System

This repository contains a small, independently runnable two-service system built as a C#/.NET technical assignment. It registers users through an HTTP API and eventually delivers registration emails through an asynchronous Kafka pipeline.

The implementation deliberately favors explicit, maintainable code over unnecessary enterprise infrastructure.

## The two services

![A single figure composed of two contrasting halves](docs/two-services.png)

*Figure 1. A compact visual description of the system's two primary projects/services: UserService and NotificationService.*

### UserService

`UserService.Api` is an ASP.NET Core Web API responsible for:

- registering users;
- retrieving a user by ID or email;
- returning an offset-paginated user list;
- validating and normalizing user data;
- enforcing unique email and phone-number constraints;
- atomically storing a user and a `user.registered` Outbox message;
- publishing pending Outbox messages to Kafka with Quartz.

The service is split into four focused projects:

```text
UserService.Api             HTTP host, controllers, middleware, composition root
UserService.Application     Use cases, Result model, ports, integration-event contract
UserService.Domain          User entity and domain rules
UserService.Infrastructure  EF Core, PostgreSQL, Outbox, Quartz, Kafka producer
```

Dependencies point inward: `Application` references `Domain`; `Infrastructure` references `Application` and `Domain`; `Api` references `Application` and `Infrastructure`. The Domain project has no dependency on infrastructure or the HTTP host.

### NotificationService

`NotificationService` is a .NET Worker Service responsible for:

- consuming `user.registered` events from Kafka sequentially;
- validating Kafka headers and deserializing versioned event payloads;
- checking the Inbox for already processed event IDs;
- creating and sending a registration email through SMTP;
- recording successfully handled events in its PostgreSQL Inbox;
- manually committing Kafka offsets only after successful processing.

Malformed or unsupported messages are logged and committed because this local assignment does not include a Dead Letter Queue. Transient processing failures are retried without committing the Kafka offset.

## Architecture and delivery flow

```text
Client
  |
  | HTTP
  v
UserService.Api
  |
  | one PostgreSQL transaction
  +----> User
  +----> OutboxMessage
              |
              | Quartz Outbox publisher
              v
            Kafka
              |
              | user.registered
              v
      NotificationService
          |          |
          | SMTP     | EF Core
          v          v
       Mailpit    InboxMessage
```

The User and Outbox records are committed atomically. Kafka availability therefore does not determine whether `POST /api/users` succeeds: unpublished records remain in PostgreSQL and are retried later.

Delivery is intentionally **at least once**. The NotificationService Inbox uses the integration event ID as its primary key to prevent normal Kafka redelivery from sending an already recorded event again. SMTP and PostgreSQL cannot participate in one atomic transaction, so a process crash after SMTP accepts an email but before Inbox persistence can still produce a duplicate email. This is an explicit distributed-systems tradeoff, not an exactly-once guarantee.

## Technology

- .NET 10 and ASP.NET Core
- Entity Framework Core with PostgreSQL
- Kafka in KRaft mode, without ZooKeeper
- Quartz.NET for Outbox polling
- MailKit for SMTP delivery
- Mailpit as the local SMTP sink and email UI
- Serilog for UserService structured console and rolling-file logs
- Scalar and OpenAPI for interactive API documentation
- xUnit for automated tests
- Docker Compose for the complete local environment

## Prerequisites

- Docker Desktop with Docker Compose
- .NET 10 SDK for host-side builds, tests, and migrations

## Run with Docker Compose

Start or rebuild the complete environment:

```bash
docker compose up -d --build
```

On a fresh database volume, restore the repository-local EF tool and apply both schemas before registering users:

```bash
dotnet tool restore
dotnet ef database update --project src/UserService.Infrastructure/UserService.Infrastructure.csproj --startup-project src/UserService.Api/UserService.Api.csproj -- --environment Development
dotnet ef database update --project src/NotificationService/NotificationService.csproj --startup-project src/NotificationService/NotificationService.csproj -- --environment Development
```

Migrations are deliberately not executed by application startup. The `notification-db-init` one-shot container creates the separate `notifications` database; EF migrations own its schema.

Check container state and follow application logs:

```bash
docker compose ps -a
docker compose logs -f user-service
docker compose logs -f notification-service
```

Stop containers while preserving PostgreSQL and Kafka data:

```bash
docker compose down
```

Delete containers and all local persisted infrastructure data:

```bash
docker compose down --volumes
```

## Local endpoints and ports

| Component | Address | Purpose |
|---|---|---|
| UserService | <http://localhost:8080> | HTTP API |
| Health | <http://localhost:8080/health> | Lightweight liveness endpoint |
| Scalar | <http://localhost:8080/scalar/v1> | Interactive API reference in Development |
| OpenAPI | <http://localhost:8080/openapi/v1.json> | OpenAPI document in Development |
| Mailpit | <http://localhost:8025> | Captured registration emails |
| Kafka UI | <http://localhost:8081> | Topics, messages, and consumer groups |
| PostgreSQL | `localhost:5432` | `users` and `notifications` databases |
| Kafka | `localhost:19092` | Host-side broker connection |

Containers use Compose DNS names such as `postgres`, `kafka`, and `mail`; `localhost` is used only by tools running on the host.

Mailpit captures messages locally. An email addressed to a real mailbox appears in the Mailpit UI and is **not** relayed to the public email provider.

## API

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/users` | Register a user |
| `GET` | `/api/users/{id}` | Find a user by ID |
| `GET` | `/api/users/by-email/{email}` | Find a user by email |
| `GET` | `/api/users?take=20&skip=0` | Return an offset-paginated user list |
| `GET` | `/health` | Return process liveness |

Example registration request:

```bash
curl -X POST http://localhost:8080/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Jane",
    "lastName": "Doe",
    "middleName": null,
    "email": "jane@example.com",
    "phoneNumber": "+380501234567"
  }'
```

A successful response is `201 Created` and includes the new ID. The `Location` header points to `GET /api/users/{id}`. The registration email should then appear in Mailpit after the Outbox publisher and NotificationService process the event.

## PostgreSQL access

Local development credentials are intentionally non-production values:

```text
Host: localhost
Port: 5432
Username: app
Password: local-dev-password
Databases: users, notifications
```

Connect through the running container:

```bash
docker compose exec postgres psql -U app -d users
docker compose exec postgres psql -U app -d notifications
```

The same values can be used in DBeaver or another PostgreSQL client. Create one connection per logical database, or switch the database configured for the connection.

## Database migrations

Migrations are stored in:

- `src/UserService.Infrastructure/Migrations`
- `src/NotificationService/Migrations`

Create a migration:

```bash
dotnet ef migrations add MigrationName --project src/UserService.Infrastructure/UserService.Infrastructure.csproj --startup-project src/UserService.Api/UserService.Api.csproj --output-dir Migrations -- --environment Development
dotnet ef migrations add MigrationName --project src/NotificationService/NotificationService.csproj --startup-project src/NotificationService/NotificationService.csproj --output-dir Migrations -- --environment Development
```

List UserService migrations:

```bash
dotnet ef migrations list --project src/UserService.Infrastructure/UserService.Infrastructure.csproj --startup-project src/UserService.Api/UserService.Api.csproj -- --environment Development
```

Remove the latest unapplied migration:

```bash
dotnet ef migrations remove --project src/UserService.Infrastructure/UserService.Infrastructure.csproj --startup-project src/UserService.Api/UserService.Api.csproj -- --environment Development
```

Rollback to a migration, or to an empty schema:

```bash
dotnet ef database update PreviousMigrationName --project src/UserService.Infrastructure/UserService.Infrastructure.csproj --startup-project src/UserService.Api/UserService.Api.csproj -- --environment Development
dotnet ef database update 0 --project src/UserService.Infrastructure/UserService.Infrastructure.csproj --startup-project src/UserService.Api/UserService.Api.csproj -- --environment Development
```

## Build and test

```bash
dotnet build LigaZakon.slnx
dotnet test LigaZakon.slnx --no-build
```

The fast test suite covers domain invariants, validation and normalization, Result behavior, application workflows, controllers and middleware, EF Core mappings and repositories, Outbox processing, Kafka metadata parsing, integration-event deserialization, and notification email mapping. It does not modify the developer PostgreSQL container.

The complete Docker flow has also been manually verified:

```text
POST /api/users
  -> User + Outbox commit
  -> Kafka publish
  -> NotificationService consume
  -> Mailpit email
  -> Inbox persistence
  -> Kafka offset commit
```

## Configuration

Important environment-variable overrides use standard .NET configuration notation:

### UserService

- `ConnectionStrings__DefaultConnection`
- `Kafka__BootstrapServers`
- `Kafka__Topics__UserEvents`
- `OutboxProcessing__BatchSize`
- `OutboxProcessing__PollingIntervalSeconds`
- `OutboxProcessing__RetryDelaySeconds`

### NotificationService

- `ConnectionStrings__DefaultConnection`
- `Kafka__BootstrapServers`
- `Kafka__GroupId`
- `Kafka__Topics__UserEvents`
- `Kafka__ProcessingRetryDelaySeconds`
- `Smtp__Host`
- `Smtp__Port`
- `Smtp__SenderName`
- `Smtp__SenderAddress`
- `Smtp__UseSsl`

No production secrets should be committed. The credentials in `docker-compose.yml` exist only for local development.

## Deliberate scope boundaries

The assignment does not include authentication, authorization, a DLQ, an API gateway, service discovery, Kubernetes, Redis, a service mesh, Debezium, Kafka Connect, or exactly-once email delivery. These can be valid production concerns, but adding them here would obscure the core user-registration and reliable asynchronous-delivery design.
