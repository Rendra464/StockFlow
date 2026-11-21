# StockFlow
Order & inventory management system with server-side datatable, event-driven email (RabbitMQ), Hangfire jobs, and async exports. .NET 10 + React.

## Sprint Goal & Status
* **Sprint Goal:** Membangun fondasi sistem manajemen pesanan dan inventaris terintegrasi antara backend dan frontend.
* **Status Saat Ini:** Sprint 1 selesai (`tag v0.0.1`)

## Cara Menjalankan
1. Salin `.env.example` jadi `.env`, isi password
2. Jalankan Docker:
   ```bash
   docker compose up -d
   ```
3. Set `user-secrets` untuk connection string
4. Update database:
   ```bash
   dotnet ef database update --project src/StockFlow.Infrastructure --startup-project src/StockFlow.Api
   ```

## Versi Stack Terpasang

| Teknologi | Versi |
| :--- | :--- |
| .NET | 10 |
| EF Core | 10.0.10 |
| SQL Server | 2025 |

## Catatan Pelajaran
* Pin versi `10.0.x` untuk paket Microsoft karena NuGet memilih `11.x`.
* Buat `.env` lewat editor.
