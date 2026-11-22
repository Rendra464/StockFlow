using System.Text.RegularExpressions;
using Bogus;
using Microsoft.EntityFrameworkCore;
using StockFlow.Domain.Entities;
using System.Reflection;
using StockFlow.Domain.Enums;

namespace StockFlow.Infrastructure.Persistence.Seeding;

public static class DbSeeder
{
    public const string SystemUserEmail = "system@stockflow.local";

    private static readonly string[] CategoryNames =
    [
        "Electronics", "Home & Kitchen", "Fashion", "Beauty", "Sports",
        "Toys", "Books", "Automotive", "Groceries", "Office Supplies"
    ];

    public static async Task SeedReferenceDataAsync(AppDbContext db, CancellationToken ct = default)
    {
        await SeedSystemUserAsync(db, ct);
        await SeedCategoriesAsync(db, ct);
        await SeedProductsAsync(db, 500, ct);
        await SeedCustomersAsync(db, 5000, ct);
    }

    private static async Task SeedSystemUserAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Email == SystemUserEmail, ct)) return;

        db.Users.Add(new User
        {
            Email = SystemUserEmail,
            PasswordHash = "!", 
            FullName = "System",
            IsActive = true
        });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCategoriesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Categories.AnyAsync(ct)) return;

        db.Categories.AddRange(CategoryNames.Select(name => new Category { Name = name }));
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedProductsAsync(AppDbContext db, int count, CancellationToken ct)
    {
        if (await db.Products.AnyAsync(ct)) return;

        var categoryIds = await db.Categories.Select(c => c.Id).ToListAsync(ct);
        var sku = 0;

        var faker = new Faker<Product>("id_ID")
            .UseSeed(1001)
            .RuleFor(p => p.Sku, _ => $"SKU-{++sku:D5}")
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.CategoryId, f => f.PickRandom(categoryIds))
            .RuleFor(p => p.Price, f => Math.Round(f.Random.Decimal(10_000m, 5_000_000m) / 1_000m) * 1_000m)
            .RuleFor(p => p.StockQuantity, f => f.Random.Int(0, 500))
            .RuleFor(p => p.ReorderLevel, f => f.Random.Int(10, 50))
            .RuleFor(p => p.IsActive, f => f.Random.Bool(0.95f))
            .RuleFor(p => p.CreatedAt, f => f.Date.PastOffset(2).UtcDateTime);

        db.Products.AddRange(faker.Generate(count));
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCustomersAsync(AppDbContext db, int count, CancellationToken ct)
    {
        if (await db.Customers.AnyAsync(ct)) return;

        var n = 0;

        var faker = new Faker<Customer>("id_ID")
            .UseSeed(2002)
            .RuleFor(c => c.Name, f => f.Name.FullName())
            .RuleFor(c => c.Email, (f, c) =>
            {
                var slug = Regex.Replace(c.Name.ToLowerInvariant(), "[^a-z]+", ".").Trim('.');
                return $"{slug}.{++n}@example.com"; 
            })
            .RuleFor(c => c.Phone, f => f.Phone.PhoneNumber())
            .RuleFor(c => c.Address, f => f.Address.StreetAddress())
            .RuleFor(c => c.City, f => f.Address.City())
            .RuleFor(c => c.CreatedAt, f => f.Date.PastOffset(2).UtcDateTime);

        db.Customers.AddRange(faker.Generate(count));
        await db.SaveChangesAsync(ct);
    }

    private sealed record ProductInfo(int Id, decimal Price);

    private static readonly PropertyInfo StatusProp = typeof(Order).GetProperty(nameof(Order.Status))!;
    private static readonly PropertyInfo PaidAtProp = typeof(Order).GetProperty(nameof(Order.PaidAt))!;

    private static readonly string[] CancelReasons =
    [
        "Customer changed their mind",
        "Out of stock",
        "Duplicate order",
        "Wrong shipping address",
        "Payment issue"
    ];

    public static async Task SeedOrdersAsync(
        AppDbContext db, int count, Action<int, int>? onProgress = null, CancellationToken ct = default)
    {
        if (count <= 0) return;

        var systemUserId = await db.Users
            .Where(u => u.Email == SystemUserEmail)
            .Select(u => u.Id)
            .SingleAsync(ct);

        var products = await db.Products
            .Where(p => p.IsActive)
            .Select(p => new ProductInfo(p.Id, p.Price))
            .ToListAsync(ct);

        var customerIds = await db.Customers.Select(c => c.Id).ToListAsync(ct);

        if (products.Count < 5 || customerIds.Count == 0)
            throw new InvalidOperationException("Seed reference data first.");

        var firstNumber = (await db.Database
            .SqlQueryRaw<long>("SELECT NEXT VALUE FOR OrderNumberSeq AS Value")
            .ToListAsync(ct)).Single();

        #pragma warning disable EF1003
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER SEQUENCE OrderNumberSeq RESTART WITH " + (firstNumber + count), ct);
        #pragma warning restore EF1003

        var rnd = new Randomizer(3003);
        var now = DateTime.UtcNow;
        var windowStart = now.AddDays(-365);
        var spanSeconds = (now - windowStart).TotalSeconds;
        const int batchSize = 5000;

        for (var offset = 0; offset < count; offset += batchSize)
        {
            var size = Math.Min(batchSize, count - offset);
            var batch = new List<Order>(size);

            for (var i = 0; i < size; i++)
            {
                var index = offset + i;
                var createdAt = windowStart.AddSeconds(spanSeconds * (index + rnd.Double()) / count);
                batch.Add(BuildOrder(rnd, now, createdAt, firstNumber + index, systemUserId, customerIds, products));
            }

            db.Orders.AddRange(batch);
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();

            onProgress?.Invoke(offset + size, count);
        }
    }

    private static Order BuildOrder(
        Randomizer rnd, DateTime now, DateTime createdAt, long number,
        int systemUserId, List<int> customerIds, List<ProductInfo> products)
    {
        var order = new Order
        {
            OrderNumber = $"ORD-{createdAt:yyyyMMdd}-{number:D6}",
            CustomerId = rnd.ListItem(customerIds),
            CreatedBy = systemUserId,
            CreatedAt = createdAt,
            ExpiresAt = createdAt.AddHours(24)
        };

        foreach (var product in rnd.ListItems(products, rnd.Int(1, 5)))
        {
            var quantity = rnd.Int(1, 5);
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.Price,
                LineTotal = product.Price * quantity
            });
        }
        order.TotalAmount = order.Items.Sum(i => i.LineTotal);

        order.StatusHistory.Add(new OrderStatusHistory
        {
            FromStatus = null,
            ToStatus = OrderStatus.Pending,
            ChangedBy = systemUserId,
            ChangedAt = createdAt
        });

        var target = PickStatus(rnd, now - createdAt);
        var current = OrderStatus.Pending;
        var at = createdAt;

        void Step(OrderStatus to, double minHours, double maxHours, int? changedBy, string? note = null)
        {
            var next = at.AddHours(rnd.Double(minHours, maxHours));
            at = next < now ? next : now;

            order.StatusHistory.Add(new OrderStatusHistory
            {
                FromStatus = current,
                ToStatus = to,
                ChangedBy = changedBy,
                ChangedAt = at,
                Note = note
            });

            if (to == OrderStatus.Paid) PaidAtProp.SetValue(order, at);
            current = to;
        }

        switch (target)
        {
            case OrderStatus.Pending:
                break;

            case OrderStatus.Expired:
                Step(OrderStatus.Expired, 24, 24.25, null, "Payment window elapsed");
                break;

            case OrderStatus.Cancelled:
                var stage = rnd.Int(0, 2);
                if (stage >= 1) Step(OrderStatus.Paid, 0.05, 20, systemUserId);
                if (stage >= 2) Step(OrderStatus.Processing, 1, 24, systemUserId);
                Step(OrderStatus.Cancelled, 0.1, stage == 0 ? 20 : 48, systemUserId, rnd.ListItem(CancelReasons));
                break;

            default:
                OrderStatus[] happyPath =
                    [OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Completed];

                foreach (var next in happyPath)
                {
                    if (next == OrderStatus.Paid) Step(next, 0.05, 20, systemUserId);
                    else Step(next, 1, 48, systemUserId);

                    if (next == target) break;
                }
                break;
        }

        StatusProp.SetValue(order, current);
        return order;
    }

    private static OrderStatus PickStatus(Randomizer rnd, TimeSpan age)
    {
        if (age < TimeSpan.FromHours(24))
            return Pick(rnd,
                (OrderStatus.Pending, 45), (OrderStatus.Paid, 25), (OrderStatus.Processing, 15),
                (OrderStatus.Shipped, 10), (OrderStatus.Cancelled, 5));

        if (age < TimeSpan.FromDays(7))
            return Pick(rnd,
                (OrderStatus.Paid, 10), (OrderStatus.Processing, 20), (OrderStatus.Shipped, 25),
                (OrderStatus.Completed, 30), (OrderStatus.Cancelled, 7), (OrderStatus.Expired, 8));

        return Pick(rnd,
            (OrderStatus.Completed, 80), (OrderStatus.Cancelled, 10), (OrderStatus.Expired, 10));
    }

    private static T Pick<T>(Randomizer rnd, params (T Value, int Weight)[] options)
    {
        var roll = rnd.Int(1, options.Sum(o => o.Weight));
        foreach (var (value, weight) in options)
        {
            roll -= weight;
            if (roll <= 0) return value;
        }
        return options[^1].Value;
    }
}