using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Activities;

public sealed record CreateActivityCommand(
    string Title,
    string? Slug,
    string? Description,
    DateOnly ActivityDate,
    string? Location,
    string? CoverImageUrl,
    string? Category) : IRequest<ActivityDto>;

public sealed class CreateActivityCommandValidator : AbstractValidator<CreateActivityCommand>
{
    public CreateActivityCommandValidator()
    {
        _ = RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        _ = RuleFor(x => x.Slug)
            .MaximumLength(350)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug can only contain lowercase letters, numbers, and hyphens.");
        _ = RuleFor(x => x.ActivityDate).NotEmpty();
        _ = RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category is not null);
    }
}

public sealed class CreateActivityCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<CreateActivityCommand, ActivityDto>
{
    public async Task<ActivityDto> Handle(CreateActivityCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);
        var baseSlug = string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.Slugify(request.Title)
            : SlugHelper.Slugify(request.Slug);

        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            throw new InvalidOperationException("Could not generate a valid slug from the title.");
        }

        var slugExists = await context.Activities.AnyAsync(a => a.Slug == baseSlug, cancellationToken);
        var slug = slugExists
            ? SlugHelper.EnsureUnique(baseSlug, s => context.Activities.Any(a => a.Slug == s))
            : baseSlug;

        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Slug = slug,
            Description = request.Description,
            ActivityDate = request.ActivityDate,
            Location = request.Location,
            CoverImageUrl = request.CoverImageUrl,
            Status = ContentStatus.Draft,
            Category = request.Category,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _ = context.Activities.Add(activity);
        _ = await context.SaveChangesAsync(cancellationToken);

        return new ActivityDto(
            activity.Id, activity.Title, activity.Slug, activity.Description, activity.ActivityDate,
            activity.Location, activity.CoverImageUrl, activity.Status.ToApiString(), activity.Category,
            activity.CreatedBy, activity.CreatedAt, activity.UpdatedAt, 0);
    }
}
