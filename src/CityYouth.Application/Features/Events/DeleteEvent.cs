using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Events;

public sealed record DeleteEventCommand(Guid Id) : IRequest;

public sealed class DeleteEventCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<DeleteEventCommand>
{
    public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);
        var @event = await context.Events.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Event not found.");

        var isAdmin = await AuthorizationHelper.IsAdminAsync(context, userId, cancellationToken);
        AuthorizationHelper.RequireOwnerOrAdmin(userId, @event.CreatedBy, isAdmin);

        _ = context.Events.Remove(@event);
        _ = await context.SaveChangesAsync(cancellationToken);
    }
}
