using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using FluentValidation;
using MediatR;

namespace CityYouth.Application.Features.Media.ListMedia;

public sealed record ListMediaQuery(Guid? ActivityId = null, string? Type = null, int Limit = 100)
    : IRequest<List<MediaItem>>;

public sealed class ListMediaHandler(ISupabaseGateway db)
    : IRequestHandler<ListMediaQuery, List<MediaItem>>
{
    public Task<List<MediaItem>> Handle(ListMediaQuery q, CancellationToken ct)
    {
        var query = $"select=*&order=sort_order.asc&limit={Math.Clamp(q.Limit, 1, 300)}";
        if (q.ActivityId is { } id)
            query += $"&activity_id=eq.{id}";
        if (!string.IsNullOrWhiteSpace(q.Type))
            query += $"&type=eq.{q.Type}";
        return db.ListAsync<MediaItem>("media", query, ct: ct);
    }
}

public sealed record MediaInput(
    Guid? ActivityId,
    string Type,
    string Source,
    string Url,
    string? ThumbnailUrl,
    string? Title,
    string? Description,
    int SortOrder);

public sealed record CreateMediaCommand(MediaInput Input) : IRequest<MediaItem>;
public sealed record GetMediaQuery(Guid Id) : IRequest<MediaItem>;
public sealed record UpdateMediaCommand(Guid Id, MediaInput Input) : IRequest<MediaItem>;
public sealed record DeleteMediaCommand(Guid Id) : IRequest;

public sealed class MediaInputValidator : AbstractValidator<MediaInput>
{
    public MediaInputValidator()
    {
        RuleFor(x => x.Type).Must(t => t is "image" or "video");
        RuleFor(x => x.Url).NotEmpty();
    }
}

public sealed class ManageMediaHandler(
    ISupabaseGateway db, ICurrentUser user, IRoleChecker roles)
    : IRequestHandler<CreateMediaCommand, MediaItem>,
      IRequestHandler<GetMediaQuery, MediaItem>,
      IRequestHandler<UpdateMediaCommand, MediaItem>,
      IRequestHandler<DeleteMediaCommand>
{
    public async Task<MediaItem> Handle(GetMediaQuery q, CancellationToken ct) =>
        await FindAsync(q.Id, ct);
    public async Task<MediaItem> Handle(CreateMediaCommand c, CancellationToken ct)
    {
        var item = new MediaItem
        {
            ActivityId = c.Input.ActivityId,
            Type = c.Input.Type,
            Source = c.Input.Source,
            Url = c.Input.Url,
            ThumbnailUrl = c.Input.ThumbnailUrl,
            Title = c.Input.Title,
            Description = c.Input.Description,
            SortOrder = c.Input.SortOrder,
            UploadedBy = user.UserId,
        };
        return await db.InsertAsync<MediaItem>("media", item, ct);
    }

    public async Task<MediaItem> Handle(UpdateMediaCommand c, CancellationToken ct)
    {
        var item = await FindAsync(c.Id, ct);
        await EnsureCanEditAsync(item.UploadedBy, ct);
        item.ActivityId = c.Input.ActivityId;
        item.Type = c.Input.Type;
        item.Source = c.Input.Source;
        item.Url = c.Input.Url;
        item.ThumbnailUrl = c.Input.ThumbnailUrl;
        item.Title = c.Input.Title;
        item.Description = c.Input.Description;
        item.SortOrder = c.Input.SortOrder;
        await db.UpdateAsync("media", "id", c.Id.ToString(), item, ct);
        return item;
    }

    public async Task Handle(DeleteMediaCommand c, CancellationToken ct)
    {
        var item = await FindAsync(c.Id, ct);
        await EnsureCanEditAsync(item.UploadedBy, ct);
        await db.DeleteAsync("media", "id", c.Id.ToString(), ct);
    }

    private async Task<MediaItem> FindAsync(Guid id, CancellationToken ct) =>
        await db.SingleAsync<MediaItem>("media", $"select=*&id=eq.{id}", ct: ct)
        ?? throw new NotFoundException($"Media '{id}' not found.");

    private async Task EnsureCanEditAsync(string? uploadedBy, CancellationToken ct) =>
        Authorization.EnsureCanEdit(uploadedBy, user.UserId,
            user.UserId is not null && await roles.IsAdminAsync(user.UserId, ct));
}
