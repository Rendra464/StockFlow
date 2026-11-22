using Microsoft.EntityFrameworkCore;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Order> Orders { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}