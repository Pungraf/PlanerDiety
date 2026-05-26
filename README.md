# DietPlanner

DietPlanner is a .NET-based MVP for weekly meal planning, meal replacement, shopping list generation, and CSV recipe import.

## Projects

- `src/DietPlanner.Api` - ASP.NET Core API with JWT auth and SQLite persistence for local development.
- `src/DietPlanner.Domain` - domain model and business rules.
- `src/DietPlanner.Application` - use-case handlers and application services.
- `src/DietPlanner.Infrastructure` - EF Core persistence and infrastructure adapters.
- `src/DietPlanner.Importer` - console importer for meal data from CSV.
- `src/DietPlanner.Mobile` - .NET MAUI Android client.

## Local Development

1. Start the local API dependencies:

```powershell
docker compose up -d
```

2. Run the API:

```powershell
dotnet run --project src/DietPlanner.Api
```

3. Run the test suite:

```powershell
dotnet test DietPlanner.sln
```

The API uses `src/DietPlanner.Api/appsettings.Development.json` for local defaults.

## Simple Deployment

1. Publish the API:

```powershell
dotnet publish src/DietPlanner.Api -c Release -o .\publish\api
```

2. Copy the published files from `publish/api` to the target server.

3. On the server, provide production settings:
   `ConnectionStrings__DietPlanner`
   `Jwt__Key`
   `Jwt__Issuer`
   `Jwt__Audience`

4. Start the API on the server:

```powershell
dotnet DietPlanner.Api.dll
```

5. Point the mobile app configuration to the deployed API base URL.
