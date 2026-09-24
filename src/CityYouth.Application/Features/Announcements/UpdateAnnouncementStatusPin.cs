using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Announcements;

public sealed record UpdateAnnouncementStatusCommand(Guid Id, string Status) : IRequest<AnnouncementDto>;

public sealed class UpdateAnnouncementStatusCommandValidator : AbstractValidator<UpdateAnnouncementStatusCommand>
{
    public UpdateAnnouncementStatusCommandValidator()
    {
        _ = RuleFor(x => x.Id).NotEmpty();
        _ = RuleFor(x => x.Status).Must(s => ContentStatusExtensions.TryParseApiString(s, out _)).WithMessage("Status must be draft, published, or archived.");
    }
}

public sealed class UpdateAnnouncementStatusCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<UpdateAnnouncementStatusCommand, AnnouncementDto>
{
    public async Task<AnnouncementDto> Handle(UpdateAnnouncementStatusCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);
        var announcement = await context.Announcements.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Announcement not found.");

        var isAdmin = await AuthorizationHelper.IsAdminAsync(context, userId, cancellationToken);
        AuthorizationHelper.RequireOwnerOrAdmin(userId, announcement.CreatedBy, isAdmin);

        if (!ContentStatusExtensions.TryParseApiString(request.Status, out var status))
        {
            throw new InvalidOperationException("Status must be draft, published, or archived.");
        }

        switch (status)
        {
            case ContentStatus.Published: announcement.Publish(); break;
            case ContentStatus.Archived: announcement.Archive(); break;
            default: announcement.Draft(); break;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return new AnnouncementDto(
            announcement.Id, announcement.Title, announcement.Slug, announcement.Content,
            announcement.CoverImageUrl, announcement.PublishedAt, announcement.IsPinned,
            announcement.Status.ToApiString(), announcement.CreatedBy, announcement.CreatedAt, announcement.UpdatedAt);
    }
}

public sealed record UpdateAnnouncementPinCommand(Guid Id, bool IsPinned) : IRequest<AnnouncementDto>;

public sealed class UpdateAnnouncementPinCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<UpdateAnnouncementPinCommand, AnnouncementDto>
{
    public async Task<AnnouncementDto> Handle(UpdateAnnouncementPinCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);
        var announcement = await context.Announcements.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Announcement not found.");

        var isAdmin = await AuthorizationHelper.IsAdminAsync(context, userId, cancellationToken);
        AuthorizationHelper.RequireOwnerOrAdmin(userId, announcement.CreatedBy, isAdmin);

        announcement.IsPinned = request.IsPinned;
        announcement.UpdatedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        return new AnnouncementDto(
            announcement.Id, announcement.Title, announcement.Slug, announcement.Content,
            announcement.CoverImageUrl, announcement.PublishedAt, announcement.IsPinned,
            announcement.Status.ToApiString(), announcement.CreatedBy, announcement.CreatedAt, announcement.UpdatedAt);
    }
}
