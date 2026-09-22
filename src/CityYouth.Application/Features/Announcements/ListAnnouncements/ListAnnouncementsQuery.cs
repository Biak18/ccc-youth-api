using CityYouth.Application.Abstractions;
using CityYouth.Domain.Entities;
using MediatR;

namespace CityYouth.Application.Features.Announcements.ListAnnouncements;

public sealed record ListAnnouncementsQuery(int Limit = 20, string Status = "published") : IRequest<List<Announcement>>;

public sealed class ListAnnouncementsHandler(ISupabaseGateway db)
    : IRequestHandler<ListAnnouncementsQuery, List<Announcement>>
{
    public Task<List<Announcement>> Handle(ListAnnouncementsQuery q, CancellationToken ct)
    {
        var status = q.Status == "any" ? string.Empty : "&status=eq.published";
        return db.ListAsync<Announcement>("announcements",
            $"select=*&order=is_pinned.desc,published_at.desc{status}" +
            $"&limit={Math.Clamp(q.Limit, 1, 100)}", ct: ct);
    }
}
