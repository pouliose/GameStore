# GameStore API

A minimal ASP.NET Core Web API for managing video games. The project targets .NET 9 and currently exposes game endpoints.

## Project Structure

- `GameStore.Api/` - Main API project
- `GameStore.Api/Dtos/` - Request and response data-transfer objects
- `GameStore.Api/Endpoints/` - Minimal API endpoint mappings
- `GameStore.Api/Models/` - Domain models such as `Game` and `Genre`
- `GameStore.Api/Validation/` - Endpoint validation filters
- `GameStore.Api/games.http` - Sample HTTP requests

## Requirements

- .NET 9 SDK
- Docker Desktop with the Docker Compose plugin
- SQL Server for Entity Framework Core database work

## SQL Server VS Code Extension

Install Microsoft's `SQL Server (mssql)` extension to connect to and manage the database from VS Code:

```powershell
code --install-extension ms-mssql.mssql
```

After starting the SQL Server container, open the SQL Server extension in VS Code and connect with:

- Server: `localhost,1433`
- Authentication: `SQL Login`
- User name: `sa`
- Password: `YourStrongPassword123!`
- Database: `GameStoreDb`

## Run the API

From the `GameStore.Api` project directory:

```powershell
dotnet restore
dotnet run
```

The API endpoints are available under `/games`.

## Run Tests

From the solution directory, run:

```powershell
dotnet test GameStore.slnx
```

The API integration tests use an isolated SQLite in-memory database and do not require the development SQL Server container.

## Production Configuration and Logging

The API writes structured JSON logs to stdout and to daily rolling files under `logs/`, retaining the most recent 14 files. Application logs include game create, update, delete, and database migration events. Failed requests include original JSON request and response bodies, each capped at 4 KB; larger request bodies are omitted and larger response bodies are marked as truncated. Bodies are not redacted, so credentials or other sensitive values in them are written to the log files. Restrict log access and retention accordingly. In production, collect stdout/stderr with the hosting platform or a centralized log provider. If the API runs in a container and file logs must survive container replacement, mount `logs/` to persistent storage; the current Compose file only runs SQL Server.

Set the production database connection string through configuration rather than committing it to `appsettings.json`. For example, in a deployment environment set `ConnectionStrings__GameStoreConnection` to the connection string supplied by your secret manager. The checked-in connection string is limited to Development settings for local use.

## Install Entity Framework Core SQL Server

Run these commands from the `GameStore.Api` project directory:

```powershell
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.0
dotnet tool install --global dotnet-ef --version 9.0.0
```

If `dotnet-ef` is already installed, update it instead:

```powershell
dotnet tool update --global dotnet-ef --version 9.0.0
```

## Entity Framework Core Migrations

After configuring a `DbContext` and SQL Server connection string, create and apply the database migration:

```powershell
dotnet ef migrations add InitialCreate --output-dir Data\Migrations
dotnet ef database update
```
