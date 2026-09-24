using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Events;

public sealed record UpdateEventCommand(
    Guid Id,
    string Title,
    string? Description,
    string? CoverImageUrl,
    string? Location,
    DateTime StartDate,
    DateTime? EndDate,
    string? RegistrationUrl,
    string? ContactInformation) : IRequest<EventDto>;

public sealed class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator()
    {
        _ = RuleFor(x => x.Id).NotEmpty();
        _ = RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        _ = RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
    }
}

public sealed class UpdateEventCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<UpdateEventCommand, EventDto>
{
    public async Task<EventDto> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);
        var @event = await context.Events.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Event not found.");

        var isAdmin = await AuthorizationHelper.IsAdminAsync(context, userId, cancellationToken);
        AuthorizationHelper.RequireOwnerOrAdmin(userId, @event.CreatedBy, isAdmin);

        @event.Title = request.Title.Trim();
        @event.Description = request.Description;
        @event.CoverImageUrl = request.CoverImageUrl;
        @event.Location = request.Location;
        @event.StartDate = request.StartDate;
        @event.EndDate = request.EndDate;
        @event.RegistrationUrl = request.RegistrationUrl;
        @event.ContactInformation = request.ContactInformation;
        @event.UpdatedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        return new EventDto(
            @event.Id, @event.Title, @event.Slug, @event.Description, @event.CoverImageUrl,
            @event.Location, @event.StartDate, @event.EndDate, @event.RegistrationUrl,
            @event.ContactInformation, @event.Status.ToApiString(), @event.CreatedBy, @event.CreatedAt, @event.UpdatedAt);
    }
}
