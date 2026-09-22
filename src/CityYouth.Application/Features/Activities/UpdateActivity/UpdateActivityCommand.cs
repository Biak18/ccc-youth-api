using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using FluentValidation;
using MediatR;

namespace CityYouth.Application.Features.Activities.UpdateActivity;

public sealed record UpdateActivityCommand(
    Guid Id,
    string Title,
    string? Slug,
    string? Description,
    DateOnly ActivityDate,
    string? Category,
    string? Location,
    string? CoverImageUrl,
    string Status) : IRequest<Activity>;

public sealed class UpdateActivityValidator : AbstractValidator<UpdateActivityCommand>
{
    public UpdateActivityValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status).Must(ContentStatuses.IsValid)
            .WithMessage("Status must be draft, published or archived.");
    }
}

public sealed class UpdateActivityHandler(
    ISupabaseGateway db, ICurrentUser user, IRoleChecker roles)
    : IRequestHandler<UpdateActivityCommand, Activity>
{
    public async Task<Activity> Handle(UpdateActivityCommand c, CancellationToken ct)
    {
        var item = await db.SingleAsync<Activity>("activities",
            $"select=*&id=eq.{c.Id}", ct: ct)
            ?? throw new NotFoundException($"Activity '{c.Id}' not found.");

        Authorization.EnsureCanEdit(item.CreatedBy, user.UserId,
            user.UserId is not null && await roles.IsAdminAsync(user.UserId, ct));

        item.Title = c.Title.Trim();
        item.Slug = await SlugHelper.UniqueAsync(db, "activities",
            SlugHelper.FromTitle(c.Title, c.Slug), c.Id, ct);
        item.Description = c.Description;
        item.ActivityDate = c.ActivityDate;
        item.Category = c.Category;
        item.Location = c.Location;
        item.CoverImageUrl = c.CoverImageUrl;
        item.SetStatus(c.Status);
        item.Touch();

        await db.UpdateAsync("activities", "id", c.Id.ToString(), item, ct);
        return item;
    }
}

public sealed record SetActivityStatusCommand(Guid Id, string Status) : IRequest<Activity>;

public sealed class SetActivityStatusHandler(
    ISupabaseGateway db, ICurrentUser user, IRoleChecker roles)
    : IRequestHandler<SetActivityStatusCommand, Activity>
{
    public async Task<Activity> Handle(SetActivityStatusCommand c, CancellationToken ct)
    {
        var item = await db.SingleAsync<Activity>("activities",
            $"select=*&id=eq.{c.Id}", ct: ct)
            ?? throw new NotFoundException($"Activity '{c.Id}' not found.");

        Authorization.EnsureCanEdit(item.CreatedBy, user.UserId,
            user.UserId is not null && await roles.IsAdminAsync(user.UserId, ct));

        item.SetStatus(c.Status);
        item.Touch();
        await db.UpdateAsync("activities", "id", c.Id.ToString(), item, ct);
        return item;
    }
}

public sealed record DeleteActivityCommand(Guid Id) : IRequest;

public sealed class DeleteActivityHandler(
    ISupabaseGateway db, ICurrentUser user, IRoleChecker roles)
    : IRequestHandler<DeleteActivityCommand>
{
    public async Task Handle(DeleteActivityCommand c, CancellationToken ct)
    {
        var item = await db.SingleAsync<Activity>("activities",
            $"select=id,created_by&id=eq.{c.Id}", ct: ct)
            ?? throw new NotFoundException($"Activity '{c.Id}' not found.");

        Authorization.EnsureCanEdit(item.CreatedBy, user.UserId,
            user.UserId is not null && await roles.IsAdminAsync(user.UserId, ct));

        var media = await db.ListAsync<MediaItem>("media",
            $"select=id&activity_id=eq.{c.Id}", ct: ct);
        foreach (var m in media)
            await db.DeleteAsync("media", "id", m.Id.ToString(), ct);

        await db.DeleteAsync("activities", "id", c.Id.ToString(), ct);
    }
}
