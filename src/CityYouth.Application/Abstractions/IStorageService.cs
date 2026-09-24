namespace CityYouth.Application.Abstractions;

public interface IStorageService
{
    IReadOnlySet<string> AllowedBuckets { get; }

    // Uploads act as the logged-in staff member: the caller's access token is
    // forwarded to Supabase Storage, so the staff-only RLS policies apply and
    // the stored object is owned by the uploader. No service-role key needed.
    Task<string> UploadAsync(
        string bucket,
        string fileName,
        Stream content,
        string contentType,
        string accessToken,
        CancellationToken cancellationToken);
}
