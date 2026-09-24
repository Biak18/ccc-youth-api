using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CityYouth.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace CityYouth.Infrastructure.Storage;

// Cloudinary (signed upload) behind the same IStorageService abstraction, so
// controllers are untouched. Media stays reachable from networks where
// *.supabase.co is blocked: browsers load res.cloudinary.com URLs directly,
// and uploads go browser -> API -> Cloudinary (signed server-side, secrets
// never leave the backend).
public sealed class CloudinaryStorageService(
    HttpClient httpClient,
    IConfiguration configuration) : IStorageService
{
    private static readonly HashSet<string> Buckets =
        new(StringComparer.OrdinalIgnoreCase) { "branding", "images", "thumbnails", "videos" };

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/gif",
            "video/mp4", "video/webm", "video/quicktime",
        };

    public IReadOnlySet<string> AllowedBuckets => Buckets;

    public async Task<string> UploadAsync(
        string bucket,
        string fileName,
        Stream content,
        string contentType,
        string accessToken,
        CancellationToken cancellationToken)
    {
        // accessToken is the caller's Supabase JWT (kept for interface parity);
        // Cloudinary trust comes from the request signature below, and the
        // [Authorize] on the controller already guarantees a signed-in caller.
        _ = accessToken;

        if (!Buckets.Contains(bucket))
        {
            throw new InvalidOperationException(
                $"Unknown bucket '{bucket}'. Allowed: {string.Join(", ", Buckets)}.");
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException($"Unsupported content type '{contentType}'.");
        }

        if (content.Length > 50 * 1024 * 1024)
        {
            throw new InvalidOperationException("File too large. Maximum size is 50 MB.");
        }

        var cloud = configuration["Cloudinary:Name"] ?? configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloud) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException(
                "Cloudinary is not configured (Cloudinary:Name/ApiKey/ApiSecret).");
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var folder = $"cityyouth/{bucket.ToLowerInvariant()}";

        var signature = Sign(
            new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["folder"] = folder,
                ["timestamp"] = timestamp,
            },
            apiSecret);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(apiKey), "api_key");
        form.Add(new StringContent(timestamp), "timestamp");
        form.Add(new StringContent(folder), "folder");
        form.Add(new StringContent(signature), "signature");

        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(streamContent, "file", fileName);

        // resource_type=auto accepts both images and videos on one endpoint.
        var response = await httpClient.PostAsync(
            $"https://api.cloudinary.com/v1_1/{cloud}/auto/upload",
            form,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Upload failed ({response.StatusCode}): {body}");
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("secure_url", out var url) &&
                url.GetString() is { Length: > 0 } secureUrl)
            {
                return secureUrl;
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Upload failed: unexpected Cloudinary response: {body}", ex);
        }

        throw new InvalidOperationException($"Upload failed: unexpected Cloudinary response: {body}");
    }

    // Cloudinary signature = SHA-1 hex of alphabetically-joined params + secret.
    // (file, api_key, resource_type and cloud_name are never part of it.)
    private static string Sign(IDictionary<string, string> parameters, string apiSecret)
    {
        var payload = string.Join(
                "&",
                parameters.OrderBy(p => p.Key, StringComparer.Ordinal)
                    .Select(p => $"{p.Key}={p.Value}")) + apiSecret;

        return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(payload)))
            .ToLowerInvariant();
    }
}
