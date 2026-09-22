using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using FluentValidation;
using MediatR;

namespace CityYouth.Application.Features.Announcements.ManageAnnouncements;

public sealed record AnnouncementInput(
    string Title,
    string? Slug,
    string? Content,
    string? CoverImageUrl,
    DateTimeOffset? PublishedAt,
    bool IsPinned,
    string Status);

public sealed record CreateAnnouncementCommand(AnnouncementInput Input) : IRequest<Announcement>;
public sealed record UpdateAnnouncementCommand(Guid Id, AnnouncementInput Input) : IRequest<Announcement>;
public sealed record SetAnnouncementStatusCommand(Guid Id, string Status) : IRequest<Announcement>;
public sealed record SetAnnouncementPinCommand(Guid Id, bool IsPinned) : IRequest<Announcement>;
public sealed record DeleteAnnouncementCommand(Guid Id) : IRequest;

public sealed class AnnouncementInputValidator : AbstractValidator<AnnouncementInput>
{
    public AnnouncementInputValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status).Must(ContentStatuses.IsValid)
            .WithMessage("Status must be draft, published or archived.");
    }
}

public sealed class ManageAnnouncementsHandler(
    ISupabaseGateway db, ICurrentUser user, IRoleChecker roles)
    : IRequestHandler<CreateAnnouncementCommand, Announcement>,
      IRequestHandler<UpdateAnnouncementCommand, Announcement>,
      IRequestHandler<SetAnnouncementStatusCommand, Announcement>,
      IRequestHandler<SetAnnouncementPinCommand, Announcement>,
      IRequestHandler<DeleteAnnouncementCommand>
{
    public async Task<Announcement> Handle(CreateAnnouncementCommand c, CancellationToken ct)
    {
        var item = Announcement.Create(c.Input.Title, user.UserId);
        Apply(item, c.Input);
        item.Slug = await SlugHelper.UniqueAsync(db, "announcements",
            SlugHelper.FromTitle(c.Input.Title, c.Input.Slug), ct: ct);
        item.SetStatus(c.Input.Status);
        if (c.Input.Status == ContentStatuses.Published && c.Input.PublishedAt is null)
            item.PublishedAt = DateTimeOffset.UtcNow;
        return await db.InsertAsync<Announcement>("announcements", item, ct);
    }

    public async Task<Announcement> Handle(UpdateAnnouncementCommand c, CancellationToken ct)
    {
        var item = await FindAsync(c.Id, ct);
        await EnsureCanEditAsync(item.CreatedBy, ct);
        Apply(item, c.Input);
        item.Slug = await SlugHelper.UniqueAsync(db, "announcements",
            SlugHelper.FromTitle(c.Input.Title, c.Input.Slug), c.Id, ct);
        item.SetStatus(c.Input.Status);
        item.Touch();
        await db.UpdateAsync("announcements", "id", c.Id.ToString(), item, ct);
        return item;
    }

    public async Task<Announcement> Handle(SetAnnouncementStatusCommand c, CancellationToken ct)
    {
        var item = await FindAsync(c.Id, ct);
        await EnsureCanEditAsync(item.CreatedBy, ct);
        item.SetStatus(c.Status);
        item.Touch();
        await db.UpdateAsync("announcements", "id", c.Id.ToString(), item, ct);
        return item;
    }

    public async Task<Announcement> Handle(SetAnnouncementPinCommand c, CancellationToken ct)
    {
        var item = await FindAsync(c.Id, ct);
        await EnsureCanEditAsync(item.CreatedBy, ct);
        item.IsPinned = c.IsPinned;
        item.Touch();
        await db.UpdateAsync("announcements", "id", c.Id.ToString(), item, ct);
        return item;
    }

    public async Task Handle(DeleteAnnouncementCommand c, CancellationToken ct)
    {
        var item = await FindAsync(c.Id, ct);
        await EnsureCanEditAsync(item.CreatedBy, ct);
        await db.DeleteAsync("announcements", "id", c.Id.ToString(), ct);
    }

    private async Task<Announcement> FindAsync(Guid id, CancellationToken ct) =>
        await db.SingleAsync<Announcement>("announcements", $"select=*&id=eq.{id}", ct: ct)
        ?? throw new NotFoundException($"Announcement '{id}' not found.");

    private async Task EnsureCanEditAsync(string? createdBy, CancellationToken ct) =>
        Authorization.EnsureCanEdit(createdBy, user.UserId,
            user.UserId is not null && await roles.IsAdminAsync(user.UserId, ct));

    private static void Apply(Announcement item, AnnouncementInput input)
    {
        item.Title = input.Title.Trim();
        item.Content = input.Content;
        item.CoverImageUrl = input.CoverImageUrl;
        item.PublishedAt = input.PublishedAt;
        item.IsPinned = input.IsPinned;
    }
}
