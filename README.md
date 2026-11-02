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

Start with `dotnet run --project src/UsersAPI/UsersAPI.csproj`. `/health` reports process availability. `/swagger` shows the current HTTP endpoints.

Registration event publication belongs to Wave 2.
