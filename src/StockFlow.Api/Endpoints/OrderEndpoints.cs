using StockFlow.Application.Common;
using StockFlow.Application.Orders.GetOrders;
using Microsoft.AspNetCore.Http.HttpResults;

namespace StockFlow.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        group.MapGet("/", async Task<Results<Ok<PagedResult<OrderListItemDto>>, ValidationProblem>> ([AsParameters] GetOrdersQuery query, GetOrdersHandler handler, CancellationToken ct) =>
        {
            try
            {
                return TypedResults.Ok(await handler.HandleAsync(query, ct));
            }
            catch (RequestValidationException ex)
            {
                return TypedResults.ValidationProblem(
                    new Dictionary<string, string[]> { [ex.Field] = [ex.Message] });
            }
        });

        return app;
    }
}