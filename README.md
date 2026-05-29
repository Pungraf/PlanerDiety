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

## Import Recipes

Load the recipe catalog from CSV into the same database used by the API:

```powershell
dotnet run --project src/DietPlanner.Importer -- .\data\meals.csv "Data Source=.\dietplanner.db"
```

For PostgreSQL, pass the Railway connection string instead of the SQLite example above:

```powershell
dotnet run --project src/DietPlanner.Importer -- .\data\meals.csv "Host=...;Port=5432;Database=railway;Username=postgres;Password=..."
```

The importer prints how many meals, ingredients, and meal-ingredient rows were saved.

## Mobile App Configuration

The mobile app now ships its own configuration from `src/DietPlanner.Mobile/appsettings.mobile.json`.
Before a production build, set these values in that file:

- `ApiBaseUrl`
- `GoogleWebClientId`

The mobile app still accepts `DIETPLANNER_API_BASE_URL` as a local override. Without it, DEBUG builds fall back to the Android emulator address `https://10.0.2.2:5001/`.

For real Google sign-in on Android, also set `DIETPLANNER_GOOGLE_WEB_CLIENT_ID` to the Google OAuth web client ID used by the backend token verification flow.
The API must use the same value under `GoogleAuth__ClientId`.

For local Android builds on Windows, the current machine setup uses:

```powershell
$env:ANDROID_HOME="C:\android-sdk"
$env:ANDROID_SDK_ROOT="C:\android-sdk"
$env:JAVA_HOME="C:\Program Files\Eclipse Adoptium\jdk-21.0.6.7-hotspot"
```

Example for a deployed backend:

```powershell
$env:DIETPLANNER_API_BASE_URL="https://planer-diety-production.up.railway.app/"
$env:DIETPLANNER_GOOGLE_WEB_CLIENT_ID="1234567890-example.apps.googleusercontent.com"
$env:GoogleAuth__ClientId="1234567890-example.apps.googleusercontent.com"
```

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
   `GoogleAuth__ClientId`

4. Start the API on the server:

```powershell
dotnet DietPlanner.Api.dll
```

5. Point the mobile app configuration to the deployed API base URL.
