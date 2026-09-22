using System.Text.Json;

namespace CityYouth.Application.Abstractions;

/// <summary>
/// PostgREST + GoTrue + Storage over HTTPS. Reads use the anon key (RLS
/// applies); writes and role checks use the service key server-side while
/// the API itself enforces ownership (backend = security boundary, §49).
/// </summary>
public interface ISupabaseGateway
{
    Task<List<T>> ListAsync<T>(string table, string query, string? userJwt = null, CancellationToken ct = default);
    Task<T?> SingleAsync<T>(string table, string query, string? userJwt = null, CancellationToken ct = default) where T : class;
    Task<long> CountAsync(string table, string query, CancellationToken ct = default);
    Task<T> InsertAsync<T>(string table, object payload, CancellationToken ct = default) where T : class;
    Task UpdateAsync(string table, string idColumn, string id, object payload, CancellationToken ct = default);
    Task DeleteAsync(string table, string idColumn, string id, CancellationToken ct = default);
    Task<string> UploadAsync(string bucket, string folder, string fileName, byte[] bytes, string contentType, CancellationToken ct = default);
    Task<JsonElement> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<JsonElement> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task<string?> GetUserRoleAsync(string userId, CancellationToken ct = default);
}

/// <summary>Caller identity from the validated Supabase access token.</summary>
public interface ICurrentUser
{
    string? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}

/// <summary>Server-side role decision (service key, never client-supplied).</summary>
public interface IRoleChecker
{
    Task<bool> IsAdminAsync(string userId, CancellationToken ct = default);
}
