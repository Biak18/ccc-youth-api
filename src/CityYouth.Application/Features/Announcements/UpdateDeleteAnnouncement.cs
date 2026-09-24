using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Announcements;

public sealed record UpdateAnnouncementCommand(
    Guid Id,
    string Title,
    string? Content,
    string? CoverImageUrl) : IRequest<AnnouncementDto>;

public sealed class UpdateAnnouncementCommandValidator : AbstractValidator<UpdateAnnouncementCommand>
{
    public UpdateAnnouncementCommandValidator()
    {
        _ = RuleFor(x => x.Id).NotEmpty();
        _ = RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
    }
}

public sealed class UpdateAnnouncementCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<UpdateAnnouncementCommand, AnnouncementDto>
{
    public async Task<AnnouncementDto> Handle(UpdateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);
        var announcement = await context.Announcements.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Announcement not found.");

        var isAdmin = await AuthorizationHelper.IsAdminAsync(context, userId, cancellationToken);
        AuthorizationHelper.RequireOwnerOrAdmin(userId, announcement.CreatedBy, isAdmin);

        announcement.Title = request.Title.Trim();
        announcement.Content = request.Content;
        announcement.CoverImageUrl = request.CoverImageUrl;
        announcement.UpdatedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        return new AnnouncementDto(
            announcement.Id, announcement.Title, announcement.Slug, announcement.Content,
            announcement.CoverImageUrl, announcement.PublishedAt, announcement.IsPinned,
            announcement.Status.ToApiString(), announcement.CreatedBy, announcement.CreatedAt, announcement.UpdatedAt);
    }
}

public sealed record DeleteAnnouncementCommand(Guid Id) : IRequest;

public sealed class DeleteAnnouncementCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<DeleteAnnouncementCommand>
{
    public async Task Handle(DeleteAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);
        var announcement = await context.Announcements.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Announcement not found.");

        var isAdmin = await AuthorizationHelper.IsAdminAsync(context, userId, cancellationToken);
        AuthorizationHelper.RequireOwnerOrAdmin(userId, announcement.CreatedBy, isAdmin);

        _ = context.Announcements.Remove(announcement);
        _ = await context.SaveChangesAsync(cancellationToken);
    }
}
