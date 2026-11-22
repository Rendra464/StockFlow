using Shouldly;
using StockFlow.Domain.Entities;
using StockFlow.Domain.Enums;
using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Tests;

public class OrderStatusTests
{
    private static Order NewOrder() => new() { OrderNumber = "ORD-TEST-000001" };

    [Fact]
    public void New_order_starts_as_Pending()
    {
        NewOrder().Status.ShouldBe(OrderStatus.Pending);
    }

    [Fact]
    public void Happy_path_Pending_to_Completed_is_allowed()
    {
        var order = NewOrder();

        order.ChangeStatus(OrderStatus.Paid, 1);
        order.ChangeStatus(OrderStatus.Processing, 1);
        order.ChangeStatus(OrderStatus.Shipped, 1);
        order.ChangeStatus(OrderStatus.Completed, 1);

        order.Status.ShouldBe(OrderStatus.Completed);
        order.StatusHistory.Count.ShouldBe(4);
    }

    [Fact]
    public void Paying_sets_PaidAt()
    {
        var order = NewOrder();

        order.ChangeStatus(OrderStatus.Paid, 1);

        order.PaidAt.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Processing)]
    public void Pending_cannot_skip_ahead(OrderStatus target)
    {
        var order = NewOrder();

        Should.Throw<DomainException>(() => order.ChangeStatus(target, 1));
    }

    [Fact]
    public void Completed_cannot_go_back_to_Pending()
    {
        var order = NewOrder();
        order.ChangeStatus(OrderStatus.Paid, 1);
        order.ChangeStatus(OrderStatus.Processing, 1);
        order.ChangeStatus(OrderStatus.Shipped, 1);
        order.ChangeStatus(OrderStatus.Completed, 1);

        Should.Throw<DomainException>(() => order.ChangeStatus(OrderStatus.Pending, 1));
    }

    [Fact]
    public void Shipped_order_cannot_be_cancelled()
    {
        var order = NewOrder();
        order.ChangeStatus(OrderStatus.Paid, 1);
        order.ChangeStatus(OrderStatus.Processing, 1);
        order.ChangeStatus(OrderStatus.Shipped, 1);

        Should.Throw<DomainException>(() =>
            order.ChangeStatus(OrderStatus.Cancelled, 1, "Customer changed mind"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancelling_without_reason_is_rejected(string? reason)
    {
        var order = NewOrder();

        Should.Throw<DomainException>(() =>
            order.ChangeStatus(OrderStatus.Cancelled, 1, reason));

        order.Status.ShouldBe(OrderStatus.Pending);
    }

    [Fact]
    public void Cancelling_with_reason_is_recorded_in_history()
    {
        var order = NewOrder();

        order.ChangeStatus(OrderStatus.Cancelled, 1, "Out of stock");

        order.Status.ShouldBe(OrderStatus.Cancelled);
        var entry = order.StatusHistory.Single();
        entry.FromStatus.ShouldBe(OrderStatus.Pending);
        entry.ToStatus.ShouldBe(OrderStatus.Cancelled);
        entry.Note.ShouldBe("Out of stock");
    }

    [Fact]
    public void Expiring_by_system_has_no_user()
    {
        var order = NewOrder();

        order.ChangeStatus(OrderStatus.Expired, changedBy: null, note: "Payment window elapsed");

        order.StatusHistory.Single().ChangedBy.ShouldBeNull();
    }

    [Fact]
    public void Failed_transition_does_not_change_status_or_history()
    {
        var order = NewOrder();

        Should.Throw<DomainException>(() => order.ChangeStatus(OrderStatus.Shipped, 1));

        order.Status.ShouldBe(OrderStatus.Pending);
        order.StatusHistory.ShouldBeEmpty();
    }
}