using CityYouth.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CityYouth.Infrastructure.Supabase;

/// <summary>
/// HTTPS gateway to PostgREST / GoTrue / Storage. No Npgsql, no DB password.
/// Snake-case JSON maps 1:1 to Supabase columns and Domain entities.
/// </summary>
public sealed class SupabaseGateway(IHttpClientFactory http, IConfiguration config) : ISupabaseGateway
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly string[] AllowedBuckets = ["images", "thumbnails", "branding"];

    private string Url => config["Supabase:Url"]?.TrimEnd('/')
        ?? throw new InvalidOperationException("Supabase:Url is not configured.");

    private string AnonKey => config["Supabase:AnonKey"]
        ?? throw new InvalidOperationException("Supabase:AnonKey is not configured.");

    private string ServiceKey => config["Supabase:ServiceKey"]
        ?? throw new InvalidOperationException("Supabase:ServiceKey is not configured (needed for writes).");

    private HttpRequestMessage Rest(
        HttpMethod method, string path, string key, string? userJwt = null, bool single = false)
    {
        var request = new HttpRequestMessage(method, $"{Url}/{path.TrimStart('/')}");
        request.Headers.Add("apikey", key);
        if (userJwt is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userJwt);
        else if (key == ServiceKey)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        if (single)
            request.Headers.Accept.Add(new("application/vnd.pgrst.object+json"));
        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, Func<string, T> parse, CancellationToken ct)
    {
        var client = http.CreateClient("supabase");
        try
        {
            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new SupabaseException((int)response.StatusCode, body);
            return parse(body);
        }
        catch (HttpRequestException ex)
        {
            throw new SupabaseUnreachableException(ex.Message, ex);
        }
    }

    public async Task<List<T>> ListAsync<T>(string table, string query, string? userJwt = null, CancellationToken ct = default)
    {
        using var request = Rest(HttpMethod.Get, $"rest/v1/{table}?{query}", AnonKey, userJwt);
        return await SendAsync(request,
            body => JsonSerializer.Deserialize<List<T>>(body, Json) ?? [], ct);
    }

    public async Task<T?> SingleAsync<T>(string table, string query, string? userJwt = null, CancellationToken ct = default) where T : class
    {
        using var request = Rest(HttpMethod.Get, $"rest/v1/{table}?{query}", AnonKey, userJwt, single: true);
        try
        {
            return await SendAsync(request,
                body => JsonSerializer.Deserialize<T>(body, Json), ct);
        }
        catch (SupabaseException ex) when (ex.StatusCode == 406)
        {
            return default; // single-row query with no match
        }
    }

    public async Task<T> InsertAsync<T>(string table, object payload, CancellationToken ct = default) where T : class
    {
        using var request = Rest(HttpMethod.Post, $"rest/v1/{table}", ServiceKey);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent(JsonSerializer.Serialize(payload, Json), Encoding.UTF8, "application/json");
        var list = await SendAsync(request,
            body => JsonSerializer.Deserialize<List<T>>(body, Json), ct);
        return list?.FirstOrDefault()
            ?? throw new SupabaseException(500, "Insert returned no representation.");
    }

    public async Task UpdateAsync(string table, string idColumn, string id, object payload, CancellationToken ct = default)
    {
        using var request = Rest(HttpMethod.Patch,
            $"rest/v1/{table}?{idColumn}=eq.{Uri.EscapeDataString(id)}", ServiceKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, Json), Encoding.UTF8, "application/json");
        await SendAsync(request, _ => true, ct);
    }

    public async Task DeleteAsync(string table, string idColumn, string id, CancellationToken ct = default)
    {
        using var request = Rest(HttpMethod.Delete,
            $"rest/v1/{table}?{idColumn}=eq.{Uri.EscapeDataString(id)}", ServiceKey);
        await SendAsync(request, _ => true, ct);
    }

    public async Task<long> CountAsync(string table, string query, CancellationToken ct = default)
    {
        var client = http.CreateClient("supabase");
        try
        {
            var extra = query.Length > 0 ? $"&{query}" : string.Empty;
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{Url}/rest/v1/{table}?select=id&limit=0{extra}");
            request.Headers.Add("apikey", ServiceKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ServiceKey);
            request.Headers.Add("Prefer", "count=exact");
            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new SupabaseException((int)response.StatusCode, body);
            // Content-Range: */42
            return response.Content.Headers.ContentRange?.Length ?? 0;
        }
        catch (HttpRequestException ex)
        {
            throw new SupabaseUnreachableException(ex.Message, ex);
        }
    }

    public async Task<JsonElement> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var client = http.CreateClient("supabase");
        try
        {
            using var content = new StringContent(
                JsonSerializer.Serialize(new { refresh_token = refreshToken }, Json),
                Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"{Url}/auth/v1/token?grant_type=refresh_token")
            {
                Content = content,
            };
            request.Headers.Add("apikey", AnonKey);
            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new SupabaseException((int)response.StatusCode, body);
            return JsonDocument.Parse(body).RootElement.Clone();
        }
        catch (HttpRequestException ex)
        {
            throw new SupabaseUnreachableException(ex.Message, ex);
        }
    }
    public async Task<string> UploadAsync(
        string bucket, string folder, string fileName, byte[] bytes, string contentType,
        CancellationToken ct = default)
    {
        if (!AllowedBuckets.Contains(bucket))
            throw new ArgumentException($"Bucket must be one of: {string.Join(", ", AllowedBuckets)}");

        var safe = string.Concat(fileName.Split(Path.GetInvalidFileNameChars()));
        var path = $"{folder.Trim('/')}/{Guid.NewGuid():N}-{safe}";

        var client = http.CreateClient("supabase");
        try
        {
            using var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"{Url}/storage/v1/object/{bucket}/{path}")
            {
                Content = content,
            };
            request.Headers.Add("apikey", ServiceKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ServiceKey);
            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new SupabaseException((int)response.StatusCode, body);
            return $"{Url}/storage/v1/object/public/{bucket}/{path}";
        }
        catch (HttpRequestException ex)
        {
            throw new SupabaseUnreachableException(ex.Message, ex);
        }
    }

    public async Task<JsonElement> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var client = http.CreateClient("supabase");
        try
        {
            using var content = new StringContent(
                JsonSerializer.Serialize(new { email, password }, Json), Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"{Url}/auth/v1/token?grant_type=password")
            {
                Content = content,
            };
            request.Headers.Add("apikey", AnonKey);
            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new SupabaseException((int)response.StatusCode, body);
            return JsonDocument.Parse(body).RootElement.Clone();
        }
        catch (HttpRequestException ex)
        {
            throw new SupabaseUnreachableException(ex.Message, ex);
        }
    }

    public async Task<string?> GetUserRoleAsync(string userId, CancellationToken ct = default)
    {
        using var request = Rest(HttpMethod.Get,
            $"rest/v1/profiles?select=role&id=eq.{Uri.EscapeDataString(userId)}",
            ServiceKey, single: true);
        try
        {
            using var doc = await SendAsync(request, body => JsonDocument.Parse(body), ct);
            return doc.RootElement.TryGetProperty("role", out var r) ? r.GetString() : null;
        }
        catch (SupabaseException ex) when (ex.StatusCode == 406)
        {
            return null;
        }
    }
}

/// <summary>PostgREST/GoTrue answered with an error status.</summary>
public sealed class SupabaseException(int statusCode, string body) : Exception(body)
{
    public int StatusCode { get; } = statusCode;
}

/// <summary>Network-level failure (VPN/proxy) — mapped to 502.</summary>
public sealed class SupabaseUnreachableException(string message, Exception inner)
    : Exception(message, inner);
