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
