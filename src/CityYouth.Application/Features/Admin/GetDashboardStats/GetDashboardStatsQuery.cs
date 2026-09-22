using CityYouth.Application.Abstractions;
using MediatR;

namespace CityYouth.Application.Features.Admin.GetDashboardStats;

public sealed record DashboardStats(
    long Activities,
    long UpcomingEvents,
    long Drafts,
    long Photos,
    long Videos);

public sealed record GetDashboardStatsQuery : IRequest<DashboardStats>;

public sealed class GetDashboardStatsHandler(ISupabaseGateway db)
    : IRequestHandler<GetDashboardStatsQuery, DashboardStats>
{
    public async Task<DashboardStats> Handle(GetDashboardStatsQuery _, CancellationToken ct)
    {
        var now = Uri.EscapeDataString(DateTimeOffset.UtcNow.ToString("O"));
        var activities = db.CountAsync("activities", "status=eq.published", ct);
        var upcoming = db.CountAsync("events", $"status=eq.published&start_date=gte.{now}", ct);
        var drafts = db.CountAsync("activities", "status=eq.draft", ct);
        var photos = db.CountAsync("media", "type=eq.image", ct);
        var videos = db.CountAsync("media", "type=eq.video", ct);
        await Task.WhenAll(activities, upcoming, drafts, photos, videos);
        return new DashboardStats(
            await activities, await upcoming, await drafts, await photos, await videos);
    }
}
