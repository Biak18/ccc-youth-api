using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Events;

public sealed record UpdateEventStatusCommand(Guid Id, string Status) : IRequest<EventDto>;

public sealed class UpdateEventStatusCommandValidator : AbstractValidator<UpdateEventStatusCommand>
{
    public UpdateEventStatusCommandValidator()
    {
        _ = RuleFor(x => x.Id).NotEmpty();
        _ = RuleFor(x => x.Status).Must(s => ContentStatusExtensions.TryParseApiString(s, out _)).WithMessage("Status must be draft, published, or archived.");
    }
}

public sealed class UpdateEventStatusCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<UpdateEventStatusCommand, EventDto>
{
    public async Task<EventDto> Handle(UpdateEventStatusCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);
        var @event = await context.Events.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Event not found.");

        var isAdmin = await AuthorizationHelper.IsAdminAsync(context, userId, cancellationToken);
        AuthorizationHelper.RequireOwnerOrAdmin(userId, @event.CreatedBy, isAdmin);

        if (!ContentStatusExtensions.TryParseApiString(request.Status, out var status))
        {
            throw new InvalidOperationException("Status must be draft, published, or archived.");
        }

        switch (status)
        {
            case ContentStatus.Published: @event.Publish(); break;
            case ContentStatus.Archived: @event.Archive(); break;
            default: @event.Draft(); break;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return new EventDto(
            @event.Id, @event.Title, @event.Slug, @event.Description, @event.CoverImageUrl,
            @event.Location, @event.StartDate, @event.EndDate, @event.RegistrationUrl,
            @event.ContactInformation, @event.Status.ToApiString(), @event.CreatedBy, @event.CreatedAt, @event.UpdatedAt);
    }
}
