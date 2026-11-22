using StockFlow.Domain.Enums;

namespace StockFlow.Application.Orders.GetOrders;

public record OrderListItemDto(
    int Id,
    string OrderNumber,
    string CustomerName,
    OrderStatus Status,
    decimal TotalAmount,
    DateTime CreatedAt);