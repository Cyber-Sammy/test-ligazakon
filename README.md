# User Registration and Notification Assignment

This repository contains the bootstrap for a small two-service .NET system. Its eventual purpose is to register users and asynchronously send registration emails. At this stage, only the runnable project skeleton and local infrastructure exist; business functionality is intentionally not implemented.

## Architecture

```text
Client -> UserService -> PostgreSQL
                    \-> Transactional Outbox (planned) -> Kafka
                                                          |
                                                          v
                                              NotificationService -> Mailpit
```

- **UserService** is a minimal ASP.NET Core Web API. It currently exposes only `GET /health`. User persistence, registration endpoints, EF Core, Kafka publishing, and the outbox processor are planned.
- **NotificationService** is a .NET Worker Service. It currently logs that it started and remains running. Kafka consumption and email delivery are planned.
- **PostgreSQL** will own user and outbox persistence for UserService. No schema or migrations exist yet.
- **Kafka** runs as a single-node KRaft broker for asynchronous integration events. No topics, producers, or consumers are created yet.
- **Mailpit** is a local SMTP test server with a browser UI. NotificationService is configured with its internal SMTP address, but does not send mail yet.

The intended delivery flow will use a Transactional Outbox: UserService will insert a user and an outbox message in one database transaction, then a background publisher will eventually relay pending messages to Kafka. This is planned, not implemented. The expected delivery model is at-least-once, so consumer idempotency will be added when message handling is implemented.

## Prerequisites

- Docker with Docker Compose
- .NET 10 SDK (only needed for builds outside Docker)

## Run locally

Build and start the complete environment:

```bash
docker compose up --build
```

After startup:

- UserService health endpoint: <http://localhost:8080/health>
- Mailpit web UI: <http://localhost:8025>
- PostgreSQL: `localhost:5432` (`users` database, `app` user)

The PostgreSQL password in `docker-compose.yml` is deliberately non-production development data. Do not reuse it outside this local environment. Containers communicate through Compose service names (`postgres`, `kafka`, and `mail`), never through `localhost`.

Stop the environment while preserving database and Kafka volumes:

```bash
docker compose down
```

Stop it and delete local infrastructure data:

```bash
docker compose down --volumes
```

## Build and test without Docker

```bash
dotnet build LigaZakon.slnx
dotnet test LigaZakon.slnx --no-build
```

The test projects are intentionally empty bootstrap projects. Business-focused tests will be added with the corresponding functionality.

## Configuration placeholders

UserService accepts:

- `ConnectionStrings__DefaultConnection`
- `Kafka__BootstrapServers`

NotificationService accepts:

- `Kafka__BootstrapServers`
- `Smtp__Host`
- `Smtp__Port`

Docker Compose provides local development values for these settings. No environment-specific secrets should be committed.

## Next step

The next logical development increment is UserService persistence and registration: add the user entity, EF Core PostgreSQL context and migration, uniqueness constraints for email and phone, and the registration/query API with focused tests. Kafka, the outbox publisher, consumer logic, and email delivery should remain separate later increments.
