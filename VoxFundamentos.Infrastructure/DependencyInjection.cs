using Microsoft.Extensions.DependencyInjection;
using VoxFundamentos.Domain.Interfaces;
using VoxFundamentos.Infrastructure.Repositories;
using VoxFundamentos.Infrastructure.Scraping;

namespace VoxFundamentos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddHttpClient<FundamentusFiiScraper>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddScoped<IFiiRepository, FiiRepository>();

        return services;
    }
}
