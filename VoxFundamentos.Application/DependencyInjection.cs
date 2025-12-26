using Microsoft.Extensions.DependencyInjection;
using VoxFundamentos.Application.Interfaces;
using VoxFundamentos.Application.Services;

namespace VoxFundamentos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IFiiService, FiiService>();
        services.AddScoped<IIndicadorEconomicoService, IndicadorEconomicoService>();
        return services;
    }
}
