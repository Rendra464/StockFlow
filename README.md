# StockFlow

Order & inventory management system with server-side datatable, event-driven email (RabbitMQ), Hangfire jobs, and async exports. .NET 10 + React.

## Sprint Goal & Status

* **Sprint Goal (Sprint 2):** Seeder and Orders datatable endpoint can be tested in the browser.
* **Current Status:** Sprint 2 complete. `GET /api/orders` supports paging, whitelisted sorting, and filters, covered by integration tests. Frontend not started yet.

## Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/download)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) (must be running for the database and for integration tests)
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
5. Seed sample data (reference data plus the given number of orders):
   ```bash
   dotnet run --project src/StockFlow.Api -- seed 50000
   ```
6. Run the API:
   ```bash
   dotnet run --project src/StockFlow.Api
   ```
   The console prints the listening URLs. HTTP requests are redirected to HTTPS, so use the HTTPS port (for example `https://localhost:7171`). If the browser warns about the certificate, run `dotnet dev-certs https --trust` once.
7. Run the tests (Docker must be running, see [Testing](#testing)):
   ```bash
   dotnet test
   ```

## Seeding

`seed <count>` creates the reference data once (it is idempotent), then **adds** `<count>` orders. Running it again adds more orders.

| Data | Amount |
| :--- | :--- |
| System user | 1 (cannot log in) |
| Categories | 10 |
| Products | 500 |
| Customers | 5,000 (unique emails) |
| Orders | `<count>`, spread over the last 365 days |

Order status depends on the order's age, and each order gets a consistent status history. Data is deterministic (fixed Bogus seeds).

To start from scratch, run this in SQL Server (order matters because relationships use `Restrict`):

```sql
DELETE FROM OrderStatusHistories;
DELETE FROM OrderItems;
DELETE FROM Orders;
ALTER SEQUENCE OrderNumberSeq RESTART WITH 1;
```

## API

### `GET /api/orders`

| Parameter | Description |
| :--- | :--- |
| `page` | Page number, minimum 1 (default 1) |
| `pageSize` | Items per page, capped at 100 (default 25) |
| `sort` | Comma-separated `column:direction`, e.g. `status:asc,createdAt:desc`. Allowed columns: `createdAt`, `orderNumber`, `status`, `totalAmount`, `customerName`. Default `createdAt:desc` |
| `status` | `Pending`, `Paid`, `Processing`, `Shipped`, `Completed`, `Cancelled`, or `Expired` |
| `from`, `to` | Date range (`yyyy-MM-dd`), both inclusive, read as UTC |
| `customerId` | Only orders of this customer |
| `search` | Prefix match on order number or customer name |

Response: `{ items, totalCount, page, pageSize }`. Invalid input (unknown sort column, bad direction, `from` later than `to`, unknown status) returns `400`.

Example:

```
GET /api/orders?status=Completed&from=2026-10-01&sort=totalAmount:desc&page=2
```

The OpenAPI document is served at `/openapi/v1.json` in Development.

## Testing

* **Unit tests** (`tests/StockFlow.Domain.Tests`) cover the order status transition rules in `Order.ChangeStatus`. They need no database or Docker.
* **Integration tests** (`tests/StockFlow.Application.Tests`) run against a real SQL Server started by Testcontainers. The container is created and removed automatically, applies the real migrations, and is separate from the development database.
* The first run is slower because the image may need to be pulled. Docker Desktop must be running.

## Architecture

Clean Architecture with five projects under `src/`:

| Project | Responsibility | Depends on |
| :--- | :--- | :--- |
| `StockFlow.Domain` | Entities, enums, business rules (e.g. `Order.ChangeStatus`) | - |
| `StockFlow.Application` | Query/command handlers, `IAppDbContext` abstraction | Domain |
| `StockFlow.Infrastructure` | EF Core, `AppDbContext`, migrations, seeding | Application |
| `StockFlow.Api` | ASP.NET Core minimal API endpoints | Application, Infrastructure |
| `StockFlow.Worker` | Background worker (email, export) | Application, Infrastructure |

Handlers talk to the database through `IAppDbContext` (no repository layer) and keep HTTP concerns out of the Application layer.

## Design Decisions

* IDs are `int`, money is `decimal(18,2)`, timestamps are stored as UTC `DateTime`.
* Order status is stored as text and can only be changed through `Order.ChangeStatus`.
* An order can be cancelled only from Pending, Paid, or Processing, and a reason is required.
* Customer email is unique. All relationships use `Restrict` (no cascade delete).
* Stock can never go negative, enforced by a database check constraint as well.
* Datatable paging, sorting, and filtering run in SQL through `IQueryable` and project straight to DTOs.
* Sorting is limited to a column whitelist; raw column names from clients are never used in queries.
* Search is prefix-only (`LIKE 'text%'`) so it can use an index. Searching inside a name is intentionally not supported.
* Sorting by `status` is alphabetical, not workflow order, because status is stored as text.

## Tech Stack (Installed)

| Technology | Version |
| :--- | :--- |
| .NET | 10 |
| EF Core | 10.0.10 |
| SQL Server (Docker) | 2025 |
| Bogus | 35.x (replace with the exact version from `StockFlow.Infrastructure.csproj`) |
| xUnit | 2.9.3 |
| Shouldly | 4.3.0 |
| Testcontainers (MsSql) | 4.17.0 |

## Documentation

* [Business Requirements Document (BRD)](docs/StockFlow_Business_Requirements_Document__BRD_.pdf)

## Roadmap

| Tag | Scope | Status |
| :--- | :--- | :--- |
| `v0.0.1` | Foundation: solution, SQL Server, entities, migration | Done |
| `v0.1` | Orders datatable with large dataset | In progress (Sprint 2 done) |
| `v0.2` | Auth, master data, orders, and email | - |
| `v0.3` | Hangfire, exports, and full Docker setup | - |
| `v1.0` | Live on Azure with final README | - |

## Lessons Learned

* Pin Microsoft packages to `10.0.x`: NuGet picks `11.x` by default, which is not compatible with .NET 10.
* Create `.env` in a text editor, not through PowerShell, so special characters are not altered.
* `NU1902`/`NU1903` warnings mean a transitive package has a known vulnerability. Pin it to a patched version (e.g. `Microsoft.OpenApi` 2.7.5, `System.Security.Cryptography.Pkcs` 10.0.13).
* ASP.NET Core silently ignores unknown query parameters, so a filter that was never wired up looks like it works. After applying a change, test one behavior that proves it is active, and restart the server.
* Real-database integration tests (Testcontainers) prove SQL behavior such as paging and `LIKE` that an in-memory provider cannot.