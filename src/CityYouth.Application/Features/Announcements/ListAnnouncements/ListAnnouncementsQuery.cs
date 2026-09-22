using CityYouth.Application.Abstractions;
using CityYouth.Domain.Entities;
using MediatR;

namespace CityYouth.Application.Features.Announcements.ListAnnouncements;

public sealed record ListAnnouncementsQuery(int Limit = 20) : IRequest<List<Announcement>>;

public sealed class ListAnnouncementsHandler(ISupabaseGateway db)
    : IRequestHandler<ListAnnouncementsQuery, List<Announcement>>
{
    public Task<List<Announcement>> Handle(ListAnnouncementsQuery q, CancellationToken ct) =>
        db.ListAsync<Announcement>("announcements",
            $"select=*&status=eq.published&order=is_pinned.desc,published_at.desc" +
            $"&limit={Math.Clamp(q.Limit, 1, 100)}", ct: ct);
}
