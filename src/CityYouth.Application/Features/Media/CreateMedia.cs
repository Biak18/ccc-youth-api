using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Media;

public sealed record CreateMediaCommand(
    Guid? ActivityId,
    string Type,
    string Source,
    string Url,
    string? ThumbnailUrl,
    string? Title,
    string? Description,
    int SortOrder = 0) : IRequest<MediaDto>;

public sealed class CreateMediaCommandValidator : AbstractValidator<CreateMediaCommand>
{
    public CreateMediaCommandValidator()
    {
        _ = RuleFor(x => x.Type).Must(MediaTypes.IsValid).WithMessage("Type must be image or video.");
        _ = RuleFor(x => x.Source).Must(MediaSources.IsValid).WithMessage("Source must be storage, youtube, or external.");
        _ = RuleFor(x => x.Url).NotEmpty().MaximumLength(2000);
        _ = RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateMediaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<CreateMediaCommand, MediaDto>
{
    public async Task<MediaDto> Handle(CreateMediaCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);

        if (request.ActivityId.HasValue)
        {
            var activity = await context.Activities.FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken)
                ?? throw new KeyNotFoundException("Activity not found.");

            var isAdmin = await AuthorizationHelper.IsAdminAsync(context, userId, cancellationToken);
            AuthorizationHelper.RequireOwnerOrAdmin(userId, activity.CreatedBy, isAdmin);
        }

        var media = new Domain.Entities.Media
        {
            Id = Guid.NewGuid(),
            ActivityId = request.ActivityId,
            Type = request.Type,
            Source = request.Source,
            Url = request.Url.Trim(),
            ThumbnailUrl = request.ThumbnailUrl,
            Title = request.Title,
            Description = request.Description,
            SortOrder = request.SortOrder,
            UploadedBy = userId,
            CreatedAt = DateTime.UtcNow,
        };

        _ = context.Media.Add(media);
        _ = await context.SaveChangesAsync(cancellationToken);

        return new MediaDto(
            media.Id, media.ActivityId, media.Type, media.Source, media.Url,
            media.ThumbnailUrl, media.Title, media.Description, media.SortOrder,
            media.UploadedBy, media.CreatedAt);
    }
}
