using Microsoft.Extensions.DependencyInjection;
using StockFlow.Application.Orders.GetOrders;

namespace StockFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetOrdersHandler>();
        return services;
    }
}