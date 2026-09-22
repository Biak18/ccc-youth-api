using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using FluentValidation;
using MediatR;

namespace CityYouth.Application.Features.Events.ManageEvents;

public sealed record EventInput(
    string Title,
    string? Slug,
    string? Description,
    string? CoverImageUrl,
    string? Location,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    string? RegistrationUrl,
    string? ContactInformation,
    string Status);

public sealed record CreateEventCommand(EventInput Input) : IRequest<YouthEvent>;
public sealed record UpdateEventCommand(Guid Id, EventInput Input) : IRequest<YouthEvent>;
public sealed record SetEventStatusCommand(Guid Id, string Status) : IRequest<YouthEvent>;
public sealed record DeleteEventCommand(Guid Id) : IRequest;

public sealed class EventInputValidator : AbstractValidator<EventInput>
{
    public EventInputValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status).Must(ContentStatuses.IsValid)
            .WithMessage("Status must be draft, published or archived.");
    }
}

public sealed class ManageEventsHandler(
    ISupabaseGateway db, ICurrentUser user, IRoleChecker roles)
    : IRequestHandler<CreateEventCommand, YouthEvent>,
      IRequestHandler<UpdateEventCommand, YouthEvent>,
      IRequestHandler<SetEventStatusCommand, YouthEvent>,
      IRequestHandler<DeleteEventCommand>
{
    public async Task<YouthEvent> Handle(CreateEventCommand c, CancellationToken ct)
    {
        var item = YouthEvent.Create(c.Input.Title, c.Input.StartDate, user.UserId);
        Apply(item, c.Input);
        item.Slug = await SlugHelper.UniqueAsync(db, "events",
            SlugHelper.FromTitle(c.Input.Title, c.Input.Slug), ct: ct);
        item.SetStatus(c.Input.Status);
        return await db.InsertAsync<YouthEvent>("events", item, ct);
    }

    public async Task<YouthEvent> Handle(UpdateEventCommand c, CancellationToken ct)
    {
        var item = await FindAsync(c.Id, ct);
        await EnsureCanEditAsync(item.CreatedBy, ct);
        Apply(item, c.Input);
        item.Slug = await SlugHelper.UniqueAsync(db, "events",
            SlugHelper.FromTitle(c.Input.Title, c.Input.Slug), c.Id, ct);
        item.SetStatus(c.Input.Status);
        item.Touch();
        await db.UpdateAsync("events", "id", c.Id.ToString(), item, ct);
        return item;
    }

    public async Task<YouthEvent> Handle(SetEventStatusCommand c, CancellationToken ct)
    {
        var item = await FindAsync(c.Id, ct);
        await EnsureCanEditAsync(item.CreatedBy, ct);
        item.SetStatus(c.Status);
        item.Touch();
        await db.UpdateAsync("events", "id", c.Id.ToString(), item, ct);
        return item;
    }

    public async Task Handle(DeleteEventCommand c, CancellationToken ct)
    {
        var item = await FindAsync(c.Id, ct);
        await EnsureCanEditAsync(item.CreatedBy, ct);
        await db.DeleteAsync("events", "id", c.Id.ToString(), ct);
    }

    private async Task<YouthEvent> FindAsync(Guid id, CancellationToken ct) =>
        await db.SingleAsync<YouthEvent>("events", $"select=*&id=eq.{id}", ct: ct)
        ?? throw new NotFoundException($"Event '{id}' not found.");

    private async Task EnsureCanEditAsync(string? createdBy, CancellationToken ct) =>
        Authorization.EnsureCanEdit(createdBy, user.UserId,
            user.UserId is not null && await roles.IsAdminAsync(user.UserId, ct));

    private static void Apply(YouthEvent item, EventInput input)
    {
        item.Title = input.Title.Trim();
        item.Description = input.Description;
        item.CoverImageUrl = input.CoverImageUrl;
        item.Location = input.Location;
        item.StartDate = input.StartDate;
        item.EndDate = input.EndDate;
        item.RegistrationUrl = input.RegistrationUrl;
        item.ContactInformation = input.ContactInformation;
    }
}
