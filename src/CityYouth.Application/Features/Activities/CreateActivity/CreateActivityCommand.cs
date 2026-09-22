using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using FluentValidation;
using MediatR;

namespace CityYouth.Application.Features.Activities.CreateActivity;

public sealed record CreateActivityCommand(
    string Title,
    string? Slug,
    string? Description,
    DateOnly ActivityDate,
    string? Category,
    string? Location,
    string? CoverImageUrl,
    string Status = "draft") : IRequest<Activity>;

public sealed class CreateActivityValidator : AbstractValidator<CreateActivityCommand>
{
    public CreateActivityValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status).Must(ContentStatuses.IsValid)
            .WithMessage("Status must be draft, published or archived.");
    }
}

public sealed class CreateActivityHandler(
    ISupabaseGateway db, ICurrentUser user) : IRequestHandler<CreateActivityCommand, Activity>
{
    public async Task<Activity> Handle(CreateActivityCommand c, CancellationToken ct)
    {
        var item = Activity.Create(c.Title, c.ActivityDate, user.UserId);
        item.Description = c.Description;
        item.Category = c.Category;
        item.Location = c.Location;
        item.CoverImageUrl = c.CoverImageUrl;
        item.Slug = await SlugHelper.UniqueAsync(db, "activities",
            SlugHelper.FromTitle(c.Title, c.Slug), ct: ct);
        item.SetStatus(c.Status);
        return await db.InsertAsync<Activity>("activities", item, ct);
    }
}
