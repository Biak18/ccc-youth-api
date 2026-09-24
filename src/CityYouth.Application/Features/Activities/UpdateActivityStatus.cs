using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Activities;

public sealed record UpdateActivityStatusCommand(Guid Id, string Status) : IRequest<ActivityDto>;

public sealed class UpdateActivityStatusCommandValidator : AbstractValidator<UpdateActivityStatusCommand>
{
    public UpdateActivityStatusCommandValidator()
    {
        _ = RuleFor(x => x.Id).NotEmpty();
        _ = RuleFor(x => x.Status).Must(s => ContentStatusExtensions.TryParseApiString(s, out _)).WithMessage("Status must be draft, published, or archived.");
    }
}

public sealed class UpdateActivityStatusCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<UpdateActivityStatusCommand, ActivityDto>
{
    public async Task<ActivityDto> Handle(UpdateActivityStatusCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);
        var activity = await context.Activities
            .Include(a => a.Media)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Activity not found.");

        var isAdmin = await AuthorizationHelper.IsAdminAsync(context, userId, cancellationToken);
        AuthorizationHelper.RequireOwnerOrAdmin(userId, activity.CreatedBy, isAdmin);

        if (!ContentStatusExtensions.TryParseApiString(request.Status, out var status))
        {
            throw new InvalidOperationException("Status must be draft, published, or archived.");
        }

        switch (status)
        {
            case ContentStatus.Published: activity.Publish(); break;
            case ContentStatus.Archived: activity.Archive(); break;
            default: activity.Draft(); break;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return new ActivityDto(
            activity.Id, activity.Title, activity.Slug, activity.Description, activity.ActivityDate,
            activity.Location, activity.CoverImageUrl, activity.Status.ToApiString(), activity.Category,
            activity.CreatedBy, activity.CreatedAt, activity.UpdatedAt, activity.Media.Count);
    }
}
