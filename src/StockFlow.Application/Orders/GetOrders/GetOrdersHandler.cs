using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions;
using StockFlow.Application.Common;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Orders.GetOrders;

public class GetOrdersHandler
{
    private const int MaxPageSize = 100;

    private static readonly HashSet<string> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "createdAt", "orderNumber", "status", "totalAmount", "customerName"
    };

    private readonly IAppDbContext _db;

    public GetOrdersHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<OrderListItemDto>> HandleAsync(GetOrdersQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var sorts = ParseSort(query.Sort);

        if (query.From is not null && query.To is not null && query.From > query.To)
            throw new RequestValidationException("from", "'from' must not be later than 'to'.");

        var orders = _db.Orders.AsNoTracking();

        if (query.Status is not null)
            orders = orders.Where(o => o.Status == query.Status);

        if (query.From is not null)
        {
            var from = query.From.Value.ToDateTime(TimeOnly.MinValue);
            orders = orders.Where(o => o.CreatedAt >= from);
        }

        if (query.To is not null)
        {
            var toExclusive = query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            orders = orders.Where(o => o.CreatedAt < toExclusive);
        }

        if (query.CustomerId is not null)
            orders = orders.Where(o => o.CustomerId == query.CustomerId);

        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            if (search.Length > 100) search = search[..100];

            orders = orders.Where(o =>
                o.OrderNumber.StartsWith(search) ||
                o.Customer!.Name.StartsWith(search));
        }

        var totalCount = await orders.CountAsync(ct);

        var ordered = ApplySort(orders, sorts);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderListItemDto(
                o.Id,
                o.OrderNumber,
                o.Customer!.Name,
                o.Status,
                o.TotalAmount,
                o.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<OrderListItemDto>(items, totalCount, page, pageSize);
    }

    private static List<(string Column, bool Desc)> ParseSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return [("createdAt", true)];

        var result = new List<(string, bool)>();

        foreach (var part in sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pieces = part.Split(':', StringSplitOptions.TrimEntries);
            var column = pieces[0];

            if (!SortableColumns.Contains(column))
                throw new RequestValidationException("sort", $"Sorting by '{column}' is not allowed.");

            var direction = pieces.Length > 1 ? pieces[1].ToLowerInvariant() : "asc";
            if (direction is not ("asc" or "desc"))
                throw new RequestValidationException("sort", $"Invalid sort direction '{pieces[1]}'. Use 'asc' or 'desc'.");

            result.Add((column, direction == "desc"));
        }

        return result;
    }

    private static IOrderedQueryable<Order> ApplySort(
        IQueryable<Order> source, List<(string Column, bool Desc)> sorts)
    {
        IOrderedQueryable<Order>? ordered = null;

        foreach (var (column, desc) in sorts)
        {
            ordered = column.ToLowerInvariant() switch
            {
                "createdat" => AddOrdering(source, ordered, o => o.CreatedAt, desc),
                "ordernumber" => AddOrdering(source, ordered, o => o.OrderNumber, desc),
                "status" => AddOrdering(source, ordered, o => o.Status, desc),
                "totalamount" => AddOrdering(source, ordered, o => o.TotalAmount, desc),
                "customername" => AddOrdering(source, ordered, o => o.Customer!.Name, desc),
                _ => throw new RequestValidationException("sort", $"Sorting by '{column}' is not allowed.")
            };
        }

        return AddOrdering(source, ordered, o => o.Id, true);
    }

    private static IOrderedQueryable<Order> AddOrdering<TKey>(
        IQueryable<Order> source,
        IOrderedQueryable<Order>? current,
        Expression<Func<Order, TKey>> key,
        bool desc)
    {
        if (current is null)
            return desc ? source.OrderByDescending(key) : source.OrderBy(key);

        return desc ? current.ThenByDescending(key) : current.ThenBy(key);
    }
}