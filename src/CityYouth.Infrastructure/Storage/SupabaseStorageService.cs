using System.Net.Http.Headers;
using CityYouth.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace CityYouth.Infrastructure.Storage;

public sealed class SupabaseStorageService(
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

        var safeName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(streamContent, "file", safeName);

        // Caller JWT (not a service key): storage RLS enforces staff-only upload
        // with owner = auth.uid().
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"storage/v1/object/{bucket}/{safeName}")
        {
            Content = form,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Upload failed ({response.StatusCode}): {body}");
        }

        var supabaseUrl = configuration["Supabase:Url"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("Supabase:Url is required.");

        return $"{supabaseUrl}/storage/v1/object/public/{bucket}/{safeName}";
    }
}
