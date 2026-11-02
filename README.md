# UsersAPI

Phase 2 user service for registration, login, JWT issuance, authorization, and Phase 1 user administration.

## Build and test

```powershell
dotnet build src/UsersAPI/UsersAPI.csproj
dotnet test tests/UsersAPI.Tests/UsersAPI.Tests.csproj
```

## Configuration

Set `Jwt__Issuer`, `Jwt__Audience`, and `Jwt__SigningKey` in the process environment. The signing key must be shared securely with CatalogAPI for token validation. Do not commit it.

`ConnectionStrings__Users` optionally selects the service's SQLite database; the default is `users.db`. `BootstrapAdmin__Email` and `BootstrapAdmin__Password` optionally create one administrator for a local demonstration if the email is absent. Do not commit the password. The service creates its local schema on startup.

The service publishes `UserCreatedEvent` as JSON to the durable direct exchange `fcg.events` with routing key `user.created`. It declares the durable `notifications.user-created` queue and binding before publication so registration events wait for NotificationsAPI when that service starts later. Configure RabbitMQ with `RabbitMq__HostName`, `RabbitMq__Port`, `RabbitMq__UserName`, `RabbitMq__Password`, and optionally `RabbitMq__Exchange`. Local defaults use `localhost:5672` and the RabbitMQ development user; supply the password through the process environment.

Registration persists independently from notification delivery. If RabbitMQ cannot accept the event, registration still returns `201 Created` and the service writes an operational error containing only the user identifier. This challenge-scoped behavior does not provide guaranteed delivery.

Start with `dotnet run --project src/UsersAPI/UsersAPI.csproj`. `/health` reports process availability. `/swagger` shows the current HTTP endpoints.

## Container preparation

`Dockerfile` publishes this service from its own repository into a .NET 8 runtime image. The image runs as the built-in non-root user, listens on port 8080, and stores SQLite data under `/data`. From this repository, build with `docker build -t fcg-users-api .` when Docker is available. The sibling `Fase 2/Orchestration` repository provides the prepared Compose entry point.

The Docker Hub Personal workflow in `.github/workflows/docker-hub.yml` runs tests before publishing the public `viniciuscs84/fcg-users-api` image. The public GitHub repository is `viniciuscs84/fiap-postech-f2-fcg-users-api`. Its `DOCKERHUB_USERNAME` variable and encrypted `DOCKERHUB_TOKEN` secret are configured. The image is published with `main` and full-commit `sha-...` tags. See the orchestration README for pull commands and token rotation.
