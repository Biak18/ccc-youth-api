using CityYouth.Application.Abstractions;
using CityYouth.Infrastructure.Authentication;
using CityYouth.Infrastructure.Persistence;
using CityYouth.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CityYouth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found in configuration.");

        _ = services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        _ = services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<AppDbContext>());

        var supabaseUrl = configuration["Supabase:Url"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("Supabase:Url is required.");

        var supabaseAnonKey = configuration["Supabase:AnonKey"]
            ?? throw new InvalidOperationException("Supabase:AnonKey is required.");

        // No service-role key: auth uses the anon key and uploads forward the
        // caller's own JWT, so Supabase storage RLS (staff-only) still applies.
        _ = services.AddHttpClient<IAuthClient, SupabaseAuthClient>(client =>
        {
            client.BaseAddress = new Uri($"{supabaseUrl}/auth/v1/");
            client.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
        });

        // Media lives on Cloudinary (signed server-side). No default headers:
        // every upload is authenticated per-request by its signature.
        _ = services.AddHttpClient<IStorageService, CloudinaryStorageService>();

        return services;
    }
}
