using CityYouth.Application.Abstractions;
using CityYouth.Infrastructure.Auth;
using CityYouth.Infrastructure.Supabase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CityYouth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var url = configuration["Supabase:Url"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("Supabase:Url is required.");

        services.AddHttpClient("supabase", client =>
        {
            client.BaseAddress = new Uri(url + "/");
        });
        services.AddScoped<ISupabaseGateway, SupabaseGateway>();
        services.AddScoped<IRoleChecker, RoleChecker>();
        return services;
    }
}
