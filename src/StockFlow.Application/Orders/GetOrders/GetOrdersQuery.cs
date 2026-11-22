using StockFlow.Domain.Enums;

namespace StockFlow.Application.Orders.GetOrders;

public record GetOrdersQuery(
    int Page = 1,
    int PageSize = 25,
    string? Sort = null,
    OrderStatus? Status = null);