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
