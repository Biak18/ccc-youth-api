using CityYouth.Application.Abstractions;
using FluentValidation;
using MediatR;

namespace CityYouth.Application.Features.Uploads.UploadImage;

public sealed record UploadImageCommand(
    string Bucket,
    string Folder,
    string FileName,
    byte[] Bytes,
    string ContentType) : IRequest<string>;

public sealed class UploadImageValidator : AbstractValidator<UploadImageCommand>
{
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];

    public UploadImageValidator()
    {
        RuleFor(x => x.Bucket).Must(b => b is "images" or "thumbnails" or "branding");
        RuleFor(x => x.Folder).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.ContentType).Must(t => AllowedTypes.Contains(t))
            .WithMessage("Only JPEG, PNG, WebP or GIF images are allowed.");
        RuleFor(x => x.Bytes.Length).GreaterThan(0).LessThanOrEqualTo(15 * 1024 * 1024);
    }
}

public sealed class UploadImageHandler(ISupabaseGateway db)
    : IRequestHandler<UploadImageCommand, string>
{
    public Task<string> Handle(UploadImageCommand c, CancellationToken ct) =>
        db.UploadAsync(c.Bucket, c.Folder, c.FileName, c.Bytes, c.ContentType, ct);
}
