# GameStore API

A minimal ASP.NET Core Web API for managing video games. The project targets .NET 9 and currently exposes game and genre endpoints.

## Project Structure

```text
GameStore.Api/
|-- docker-compose.yml        # Runs the API and SQL Server
|-- GameStore.slnx            # Solution containing API and test projects
|-- GameStore.Api/
|   |-- Data/                 # EF Core context, setup, and migrations
|   |-- Dtos/                 # Request and response data-transfer objects
|   |-- Endpoints/            # Minimal API route mappings
|   |-- HttpEndpointCommannds/ # Sample .http requests for games and genres
|   |-- Middleware/           # Failed-request body capture for logging
|   |-- Models/               # Game and genre entities
|   |-- Resources/            # CSV seed data
|   |-- Validation/           # Data annotation endpoint filter
|   |-- Dockerfile            # Multi-stage API image build
|   |-- Program.cs            # Application and middleware configuration
|   `-- appsettings*.json     # Logging and environment configuration
`-- GameStore.Api.Tests/      # API integration and middleware tests
```

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

From the solution directory, start SQL Server and the API together:

```powershell
docker compose up --build
```

The API is available at `http://localhost:5225`; Swagger UI is available at `http://localhost:5225/swagger`. List endpoints (`/games`, `/genres`, and `/genres/{id}/games`) accept `page` and `pageSize` query parameters, defaulting to page 1 and 20 items per page. Page sizes are limited to 100 and offsets to 10,000 rows; responses include `items`, `page`, `pageSize`, and `totalCount`. Compose waits for SQL Server to become healthy before starting the API. Keep this command running to see container output. Stop both services with `Ctrl+C`, or run `docker compose down` from another terminal.

To override the sample SQL Server password for local development, set the environment variable before starting Compose:

```powershell
$env:MSSQL_SA_PASSWORD = "choose-a-local-password"
docker compose up --build
```

The sample password is for local demos only; use managed secrets for deployments.

## Check Logs

Follow the API's console logs, including request summaries and application events, from the solution directory:

```powershell
docker compose logs -f api
```

The API also writes daily JSON files under `/app/logs` inside the container. List the files with:

```powershell
docker compose exec api sh -c "ls -lh /app/logs"
```

Database files and API log files are stored in named Docker volumes and survive `docker compose down`. **Do not run `docker compose down -v` unless you intend to delete both the database data and the stored logs.**

## Run Tests

From the solution directory, run:

```powershell
dotnet test GameStore.slnx
```

The API integration tests use an isolated SQLite in-memory database and do not require the development SQL Server container.

## Production Configuration and Logging

The API writes structured JSON logs to stdout and to daily rolling files under `logs/`, retaining the most recent 14 files. Application logs include game create, update, delete, and database migration events. Failed requests include original JSON request and response bodies, each capped at 4 KB; larger request bodies are omitted and larger response bodies are marked as truncated. Bodies are not redacted, so credentials or other sensitive values in them are written to the log files. Restrict log access and retention accordingly. In production, collect stdout/stderr with the hosting platform or a centralized log provider. The Compose setup mounts the API log directory to a named volume so files persist across container replacement.

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
