using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Activities;

public sealed record UpdateActivityCommand(
    Guid Id,
    string Title,
    string? Description,
    DateOnly ActivityDate,
    string? Location,
    string? CoverImageUrl,
    string? Category) : IRequest<ActivityDto>;

public sealed class UpdateActivityCommandValidator : AbstractValidator<UpdateActivityCommand>
{
    public UpdateActivityCommandValidator()
    {
        _ = RuleFor(x => x.Id).NotEmpty();
        _ = RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        _ = RuleFor(x => x.ActivityDate).NotEmpty();
    }
}

public sealed class UpdateActivityCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<UpdateActivityCommand, ActivityDto>
{
    public async Task<ActivityDto> Handle(UpdateActivityCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);
        var activity = await context.Activities
            .Include(a => a.Media)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Activity not found.");

        var isAdmin = await AuthorizationHelper.IsAdminAsync(context, userId, cancellationToken);
        AuthorizationHelper.RequireOwnerOrAdmin(userId, activity.CreatedBy, isAdmin);

        activity.Title = request.Title.Trim();
        activity.Description = request.Description;
        activity.ActivityDate = request.ActivityDate;
        activity.Location = request.Location;
        activity.CoverImageUrl = request.CoverImageUrl;
        activity.Category = request.Category;
        activity.UpdatedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        return new ActivityDto(
            activity.Id, activity.Title, activity.Slug, activity.Description, activity.ActivityDate,
            activity.Location, activity.CoverImageUrl, activity.Status.ToApiString(), activity.Category,
            activity.CreatedBy, activity.CreatedAt, activity.UpdatedAt, activity.Media.Count);
    }
}
