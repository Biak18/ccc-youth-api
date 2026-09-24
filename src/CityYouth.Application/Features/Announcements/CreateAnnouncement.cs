using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Announcements;

public sealed record CreateAnnouncementCommand(
    string Title,
    string? Slug,
    string? Content,
    string? CoverImageUrl,
    bool IsPinned = false) : IRequest<AnnouncementDto>;

public sealed class CreateAnnouncementCommandValidator : AbstractValidator<CreateAnnouncementCommand>
{
    public CreateAnnouncementCommandValidator()
    {
        _ = RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        _ = RuleFor(x => x.Slug)
            .MaximumLength(350)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug can only contain lowercase letters, numbers, and hyphens.");
    }
}

public sealed class CreateAnnouncementCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<CreateAnnouncementCommand, AnnouncementDto>
{
    public async Task<AnnouncementDto> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);
        var baseSlug = string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.Slugify(request.Title)
            : SlugHelper.Slugify(request.Slug);

        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            throw new InvalidOperationException("Could not generate a valid slug from the title.");
        }

        var slugExists = await context.Announcements.AnyAsync(a => a.Slug == baseSlug, cancellationToken);
        var slug = slugExists
            ? SlugHelper.EnsureUnique(baseSlug, s => context.Announcements.Any(a => a.Slug == s))
            : baseSlug;

        var announcement = new Announcement
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Slug = slug,
            Content = request.Content,
            CoverImageUrl = request.CoverImageUrl,
            IsPinned = request.IsPinned,
            Status = ContentStatus.Draft,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _ = context.Announcements.Add(announcement);
        _ = await context.SaveChangesAsync(cancellationToken);

        return new AnnouncementDto(
            announcement.Id, announcement.Title, announcement.Slug, announcement.Content,
            announcement.CoverImageUrl, announcement.PublishedAt, announcement.IsPinned,
            announcement.Status.ToApiString(), announcement.CreatedBy, announcement.CreatedAt, announcement.UpdatedAt);
    }
}
