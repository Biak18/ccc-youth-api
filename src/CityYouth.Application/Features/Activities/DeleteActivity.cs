using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Activities;

public sealed record DeleteActivityCommand(Guid Id) : IRequest;

public sealed class DeleteActivityCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<DeleteActivityCommand>
{
    public async Task Handle(DeleteActivityCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);
        var activity = await context.Activities.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Activity not found.");

        var isAdmin = await AuthorizationHelper.IsAdminAsync(context, userId, cancellationToken);
        AuthorizationHelper.RequireOwnerOrAdmin(userId, activity.CreatedBy, isAdmin);

        _ = context.Activities.Remove(activity);
        _ = await context.SaveChangesAsync(cancellationToken);
    }
}
