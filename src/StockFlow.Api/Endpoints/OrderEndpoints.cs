using StockFlow.Application.Common;
using StockFlow.Application.Orders.GetOrders;

namespace StockFlow.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        group.MapGet("/", async (
            [AsParameters] GetOrdersQuery query,
            GetOrdersHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await handler.HandleAsync(query, ct));
            }
            catch (RequestValidationException ex)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { [ex.Field] = [ex.Message] });
            }
        });

        return app;
    }
}