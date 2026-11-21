using StockFlow.Domain.Enums;
using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Entities;

public class Order
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Paid, OrderStatus.Cancelled, OrderStatus.Expired],
        [OrderStatus.Paid] = [OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Completed],
        [OrderStatus.Completed] = [],
        [OrderStatus.Cancelled] = [],
        [OrderStatus.Expired] = []
    };

    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; private set; }
    public DateTime ExpiresAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Customer? Customer { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();

    public bool CanChangeTo(OrderStatus target) =>
        AllowedTransitions.TryGetValue(Status, out var allowed) && allowed.Contains(target);

    public void ChangeStatus(OrderStatus newStatus, int? changedBy, string? note = null)
    {
        if (!CanChangeTo(newStatus))
            throw new DomainException($"Cannot change order status from {Status} to {newStatus}.");

        if (newStatus == OrderStatus.Cancelled && string.IsNullOrWhiteSpace(note))
            throw new DomainException("A reason is required to cancel an order.");

        var from = Status;
        Status = newStatus;

        if (newStatus == OrderStatus.Paid)
            PaidAt = DateTime.UtcNow;

        StatusHistory.Add(new OrderStatusHistory
        {
            FromStatus = from,
            ToStatus = newStatus,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            Note = note
        });
    }
}