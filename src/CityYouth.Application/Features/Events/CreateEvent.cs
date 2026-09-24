using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Events;

public sealed record CreateEventCommand(
    string Title,
    string? Slug,
    string? Description,
    string? CoverImageUrl,
    string? Location,
    DateTime StartDate,
    DateTime? EndDate,
    string? RegistrationUrl,
    string? ContactInformation) : IRequest<EventDto>;

public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        _ = RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        _ = RuleFor(x => x.Slug)
            .MaximumLength(350)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug can only contain lowercase letters, numbers, and hyphens.");
        _ = RuleFor(x => x.StartDate).NotEmpty();
        _ = RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
        _ = RuleFor(x => x.RegistrationUrl).MaximumLength(2000).When(x => x.RegistrationUrl is not null);
    }
}

public sealed class CreateEventCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<CreateEventCommand, EventDto>
{
    public async Task<EventDto> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);
        var baseSlug = string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.Slugify(request.Title)
            : SlugHelper.Slugify(request.Slug);

        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            throw new InvalidOperationException("Could not generate a valid slug from the title.");
        }

        var slugExists = await context.Events.AnyAsync(e => e.Slug == baseSlug, cancellationToken);
        var slug = slugExists
            ? SlugHelper.EnsureUnique(baseSlug, s => context.Events.Any(e => e.Slug == s))
            : baseSlug;

        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Slug = slug,
            Description = request.Description,
            CoverImageUrl = request.CoverImageUrl,
            Location = request.Location,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            RegistrationUrl = request.RegistrationUrl,
            ContactInformation = request.ContactInformation,
            Status = ContentStatus.Draft,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _ = context.Events.Add(@event);
        _ = await context.SaveChangesAsync(cancellationToken);

        return new EventDto(
            @event.Id, @event.Title, @event.Slug, @event.Description, @event.CoverImageUrl,
            @event.Location, @event.StartDate, @event.EndDate, @event.RegistrationUrl,
            @event.ContactInformation, @event.Status.ToApiString(), @event.CreatedBy, @event.CreatedAt, @event.UpdatedAt);
    }
}
