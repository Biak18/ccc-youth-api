using CityYouth.Application.Abstractions;
using CityYouth.Infrastructure.Supabase;

namespace CityYouth.Infrastructure.Auth;

/// <summary>Server-side role decision from the profiles table (service key).</summary>
public sealed class RoleChecker(ISupabaseGateway gateway) : IRoleChecker
{
    public async Task<bool> IsAdminAsync(string userId, CancellationToken ct = default) =>
        await gateway.GetUserRoleAsync(userId, ct) == "admin";
}
