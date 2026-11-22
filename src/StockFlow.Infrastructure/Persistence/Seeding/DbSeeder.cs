using System.Text.RegularExpressions;
using Bogus;
using Microsoft.EntityFrameworkCore;
using StockFlow.Domain.Entities;

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
}