using Microsoft.EntityFrameworkCore;
using StockFlow.Domain.Entities;
using StockFlow.Domain.Enums;
using StockFlow.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace StockFlow.Application.Tests;

public class OrdersDbFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

    public int BudiSantosoId { get; private set; }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;
        return new AppDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
        await SeedAsync(db);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private async Task SeedAsync(AppDbContext db)
    {
        var user = new User { Email = "test@stockflow.local", PasswordHash = "!", FullName = "Test" };
        var category = new Category { Name = "Test" };
        db.Users.Add(user);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            Sku = "TEST-1",
            Name = "Test product",
            CategoryId = category.Id,
            Price = 100,
            StockQuantity = 100,
            ReorderLevel = 5
        };
        var santoso = new Customer { Name = "Budi Santoso", Email = "budi.santoso@example.com" };
        var hartono = new Customer { Name = "Budi Hartono", Email = "budi.hartono@example.com" };
        var siti = new Customer { Name = "Siti Aminah", Email = "siti.aminah@example.com" };
        db.Products.Add(product);
        db.Customers.AddRange(santoso, hartono, siti);
        await db.SaveChangesAsync();

        BudiSantosoId = santoso.Id;

        var n = 0;

        void Add(Customer customer, DateTime createdAt, decimal total, OrderStatus finalStatus)
        {
            var order = new Order
            {
                OrderNumber = $"ORD-TEST-{++n:D3}",
                CustomerId = customer.Id,
                CreatedBy = user.Id,
                CreatedAt = createdAt,
                ExpiresAt = createdAt.AddHours(24),
                TotalAmount = total,
                Items =
                {
                    new OrderItem { ProductId = product.Id, Quantity = 1, UnitPrice = total, LineTotal = total }
                }
            };

            OrderStatus[] path = finalStatus switch
            {
                OrderStatus.Pending => [],
                OrderStatus.Paid => [OrderStatus.Paid],
                OrderStatus.Completed =>
                    [OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Completed],
                OrderStatus.Cancelled => [OrderStatus.Cancelled],
                _ => throw new ArgumentOutOfRangeException(nameof(finalStatus))
            };

            foreach (var step in path)
                order.ChangeStatus(step, user.Id, step == OrderStatus.Cancelled ? "Test cancel" : null);

            db.Orders.Add(order);
        }

        static DateTime Utc(int month, int day, int hour, int minute = 0) =>
            new(2026, month, day, hour, minute, 0, DateTimeKind.Utc);

        Add(santoso, Utc(10, 1, 9), 100, OrderStatus.Pending);   
        Add(santoso, Utc(10, 1, 23, 30), 200, OrderStatus.Paid);      
        Add(santoso, Utc(10, 15, 10), 300, OrderStatus.Completed); 
        Add(hartono, Utc(10, 2, 8), 150, OrderStatus.Pending);   
        Add(hartono, Utc(11, 1, 12), 250, OrderStatus.Paid);      
        Add(siti, Utc(10, 3, 14), 120, OrderStatus.Pending);   
        Add(siti, Utc(10, 20, 16), 80, OrderStatus.Cancelled); 
        Add(siti, Utc(11, 5, 7), 500, OrderStatus.Pending);   

        await db.SaveChangesAsync();
    }
}