using Microsoft.Extensions.DependencyInjection;

namespace TradeFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(DependencyInjection).Assembly));
        return services;
    }
}
