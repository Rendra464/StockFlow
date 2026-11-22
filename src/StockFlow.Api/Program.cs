using System.Text.Json.Serialization;
using StockFlow.Api.Endpoints;
using StockFlow.Application;
using StockFlow.Infrastructure;
using StockFlow.Infrastructure.Persistence;
using StockFlow.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();


if (args.Length > 0 && args[0] == "seed")
{
    var orderCount = args.Length > 1 && int.TryParse(args[1], out var parsed) ? parsed : 0;

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await DbSeeder.SeedReferenceDataAsync(db);
    Console.WriteLine("Reference data seeded.");

    if (orderCount > 0)
    {
        await DbSeeder.SeedOrdersAsync(db, orderCount,
            (done, total) => Console.WriteLine($"Orders seeded: {done}/{total}"));
        Console.WriteLine("Done.");
    }
    return;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapOrderEndpoints();

app.Run();