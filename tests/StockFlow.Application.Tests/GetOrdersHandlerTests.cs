using Shouldly;
using StockFlow.Application.Common;
using StockFlow.Application.Orders.GetOrders;
using StockFlow.Domain.Enums;

namespace StockFlow.Application.Tests;

public class GetOrdersHandlerTests : IClassFixture<OrdersDbFixture>
{
    private readonly OrdersDbFixture _fixture;

    public GetOrdersHandlerTests(OrdersDbFixture fixture) => _fixture = fixture;

    private async Task<PagedResult<OrderListItemDto>> RunAsync(GetOrdersQuery query)
    {
        await using var db = _fixture.CreateDbContext();
        return await new GetOrdersHandler(db).HandleAsync(query);
    }

    [Fact]
    public async Task Paging_returns_correct_slices_without_overlap()
    {
        var p1 = await RunAsync(new GetOrdersQuery(Page: 1, PageSize: 3));
        var p2 = await RunAsync(new GetOrdersQuery(Page: 2, PageSize: 3));
        var p3 = await RunAsync(new GetOrdersQuery(Page: 3, PageSize: 3));

        p1.Items.Count.ShouldBe(3);
        p2.Items.Count.ShouldBe(3);
        p3.Items.Count.ShouldBe(2);
        p1.TotalCount.ShouldBe(8);

        p1.Items.Concat(p2.Items).Concat(p3.Items)
            .Select(i => i.Id).Distinct().Count().ShouldBe(8);
    }

    [Fact]
    public async Task PageSize_is_capped_and_page_is_at_least_one()
    {
        var result = await RunAsync(new GetOrdersQuery(Page: 0, PageSize: 5000));

        result.PageSize.ShouldBe(100);
        result.Page.ShouldBe(1);
    }

    [Fact]
    public async Task Default_sort_is_newest_first()
    {
        var result = await RunAsync(new GetOrdersQuery());

        result.Items[0].OrderNumber.ShouldBe("ORD-TEST-008");
    }

    [Fact]
    public async Task Sort_by_totalAmount_works_in_both_directions()
    {
        var asc = await RunAsync(new GetOrdersQuery(Sort: "totalAmount:asc"));
        var desc = await RunAsync(new GetOrdersQuery(Sort: "totalAmount:desc"));

        asc.Items[0].TotalAmount.ShouldBe(80m);
        desc.Items[0].TotalAmount.ShouldBe(500m);
    }

    [Fact]
    public async Task Multi_column_sort_is_applied_in_order()
    {
        var result = await RunAsync(new GetOrdersQuery(Sort: "status:asc,createdAt:desc"));

        result.Items.Select(i => i.OrderNumber).ShouldBe(
        [
            "ORD-TEST-007",
            "ORD-TEST-003", 
            "ORD-TEST-005", "ORD-TEST-002", 
            "ORD-TEST-008", "ORD-TEST-006", "ORD-TEST-004", "ORD-TEST-001" 
        ]);
    }

    [Theory]
    [InlineData("password:asc")]
    [InlineData("createdAt:sideways")]
    [InlineData("id; DROP TABLE Orders")]
    public async Task Sort_outside_whitelist_or_with_bad_direction_is_rejected(string sort)
    {
        await Should.ThrowAsync<RequestValidationException>(
            () => RunAsync(new GetOrdersQuery(Sort: sort)));
    }

    [Fact]
    public async Task Status_filter_returns_only_matching_orders()
    {
        var result = await RunAsync(new GetOrdersQuery(Status: OrderStatus.Pending));

        result.TotalCount.ShouldBe(4);
        result.Items.ShouldAllBe(i => i.Status == OrderStatus.Pending);
    }

    [Fact]
    public async Task Date_range_covers_the_whole_end_day()
    {
        var day = new DateOnly(2026, 10, 1);

        var result = await RunAsync(new GetOrdersQuery(From: day, To: day));

        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Date_range_for_a_month_excludes_other_months()
    {
        var result = await RunAsync(new GetOrdersQuery(
            From: new DateOnly(2026, 10, 1), To: new DateOnly(2026, 10, 31)));

        result.TotalCount.ShouldBe(6);
    }

    [Fact]
    public async Task From_later_than_to_is_rejected()
    {
        await Should.ThrowAsync<RequestValidationException>(() => RunAsync(
            new GetOrdersQuery(From: new DateOnly(2026, 11, 1), To: new DateOnly(2026, 10, 1))));
    }

    [Fact]
    public async Task Customer_filter_returns_only_that_customers_orders()
    {
        var result = await RunAsync(new GetOrdersQuery(CustomerId: _fixture.BudiSantosoId));

        result.TotalCount.ShouldBe(3);
        result.Items.ShouldAllBe(i => i.CustomerName == "Budi Santoso");
    }

    [Fact]
    public async Task Search_matches_customer_name_prefix_case_insensitively()
    {
        (await RunAsync(new GetOrdersQuery(Search: "Budi"))).TotalCount.ShouldBe(5);
        (await RunAsync(new GetOrdersQuery(Search: "budi"))).TotalCount.ShouldBe(5);
    }

    [Fact]
    public async Task Search_does_not_match_the_middle_of_a_name()
    {
        var result = await RunAsync(new GetOrdersQuery(Search: "Santoso"));

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Search_matches_order_number_prefix()
    {
        var result = await RunAsync(new GetOrdersQuery(Search: "ORD-TEST-00"));

        result.TotalCount.ShouldBe(8);
    }

    [Fact]
    public async Task Filters_combine_and_total_count_reflects_them()
    {
        var result = await RunAsync(new GetOrdersQuery(Status: OrderStatus.Pending, Search: "Budi"));

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldAllBe(i => i.Status == OrderStatus.Pending);
    }
}