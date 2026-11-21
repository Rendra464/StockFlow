# StockFlow

Order & inventory management system with server-side datatable, event-driven email (RabbitMQ), Hangfire jobs, and async exports. .NET 10 + React.

## Sprint Goal & Status

* **Sprint Goal:** Solution runs, SQL Server in Docker, tables created via migration.
* **Current Status:** Sprint 1 complete (tag `v0.0.1`). Frontend not started yet.

## Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/download)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/)
* EF Core CLI tool:
  ```bash
  dotnet tool install --global dotnet-ef --version 10.0.10
  ```

## Getting Started

Run all commands from the repository root.

1. Copy `.env.example` to `.env` and set the SQL Server password. The password must be at least 8 characters and mix uppercase, lowercase, digits, and symbols. Write it without quotes and avoid the characters `$`, `"`, `'`, `#`, and spaces.
   ```
   MSSQL_SA_PASSWORD=your-password-here
   ```
2. Start SQL Server and wait until its status is `healthy`:
   ```bash
   docker compose up -d
   docker compose ps
   ```
3. Set the connection string with user-secrets (the password must match the one in `.env`):
   ```bash
   dotnet user-secrets init --project src/StockFlow.Api
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" 'Server=localhost,1433;Database=StockFlow;User Id=sa;Password=<password-from-.env>;TrustServerCertificate=True' --project src/StockFlow.Api
   ```
4. Create the database and tables:
   ```bash
   dotnet ef database update --project src/StockFlow.Infrastructure --startup-project src/StockFlow.Api
   ```
5. Run the tests:
   ```bash
   dotnet test
   ```

## Architecture

Clean Architecture with five projects under `src/`:

| Project | Responsibility | Depends on |
| :--- | :--- | :--- |
| `StockFlow.Domain` | Entities, enums, business rules (e.g. `Order.ChangeStatus`) | - |
| `StockFlow.Application` | Use cases and handlers | Domain |
| `StockFlow.Infrastructure` | EF Core, `AppDbContext`, migrations | Application |
| `StockFlow.Api` | ASP.NET Core Web API | Application, Infrastructure |
| `StockFlow.Worker` | Background worker (email, export) | Application, Infrastructure |

## Design Decisions

* IDs are `int`, money is `decimal(18,2)`, timestamps are stored as UTC `DateTime`.
* Order status is stored as text and can only be changed through `Order.ChangeStatus`.
* An order can be cancelled only from Pending, Paid, or Processing, and a reason is required.
* Customer email is unique. All relationships use `Restrict` (no cascade delete).
* Stock can never go negative, enforced by a database check constraint as well.

## Tech Stack (Installed)

| Technology | Version |
| :--- | :--- |
| .NET | 10 |
| EF Core | 10.0.10 |
| SQL Server (Docker) | 2025 |

## Documentation

* [Business Requirements Document (BRD)](docs/StockFlow_Business_Requirements_Document__BRD_.pdf)

## Roadmap

| Tag | Scope | Status |
| :--- | :--- | :--- |
| `v0.0.1` | Foundation: solution, SQL Server, entities, migration | Done |
| `v0.1` | Orders datatable with large dataset | Next |
| `v0.2` | Auth, master data, orders, and email | - |
| `v0.3` | Hangfire, exports, and full Docker setup | - |
| `v1.0` | Live on Azure with final README | - |

## Lessons Learned

* Pin Microsoft packages to `10.0.x`: NuGet picks `11.x` by default, which is not compatible with .NET 10.
* Create `.env` in a text editor, not through PowerShell, so special characters are not altered.
* `NU1902`/`NU1903` warnings mean a transitive package has a known vulnerability. Pin it to a patched version (e.g. `Microsoft.OpenApi` 2.7.5, `System.Security.Cryptography.Pkcs` 10.0.13).