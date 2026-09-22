using CityYouth.Application.Abstractions;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using MediatR;

namespace CityYouth.Application.Features.Activities.GetActivity;

public sealed record GetActivityQuery(string Slug) : IRequest<Activity>;

public sealed class GetActivityHandler(ISupabaseGateway db)
    : IRequestHandler<GetActivityQuery, Activity>
{
    public async Task<Activity> Handle(GetActivityQuery q, CancellationToken ct)
    {
        var item = await db.SingleAsync<Activity>("activities",
            $"select=*&slug=eq.{Uri.EscapeDataString(q.Slug)}&status=in.(published,archived)", ct: ct);
        return item ?? throw new NotFoundException($"Activity '{q.Slug}' not found.");
    }
}

public sealed record GetActivityMediaQuery(Guid Id) : IRequest<List<MediaItem>>;

public sealed class GetActivityMediaHandler(ISupabaseGateway db)
    : IRequestHandler<GetActivityMediaQuery, List<MediaItem>>
{
    public Task<List<MediaItem>> Handle(GetActivityMediaQuery q, CancellationToken ct) =>
        db.ListAsync<MediaItem>("media",
            $"select=*&activity_id=eq.{q.Id}&order=sort_order.asc", ct: ct);
}
