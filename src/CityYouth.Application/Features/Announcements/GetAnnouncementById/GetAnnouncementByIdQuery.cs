using CityYouth.Application.Abstractions;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using MediatR;

namespace CityYouth.Application.Features.Announcements.GetAnnouncementById;

public sealed record GetAnnouncementByIdQuery(Guid Id) : IRequest<Announcement>;

public sealed class GetAnnouncementByIdHandler(ISupabaseGateway db)
    : IRequestHandler<GetAnnouncementByIdQuery, Announcement>
{
    public async Task<Announcement> Handle(GetAnnouncementByIdQuery q, CancellationToken ct)
    {
        var item = await db.SingleAsync<Announcement>("announcements",
            $"select=*&id=eq.{q.Id}", ct: ct);
        return item ?? throw new NotFoundException($"Announcement '{q.Id}' not found.");
    }
}
