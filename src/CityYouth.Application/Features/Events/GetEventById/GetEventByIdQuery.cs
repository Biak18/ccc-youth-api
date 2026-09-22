using CityYouth.Application.Abstractions;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using MediatR;

namespace CityYouth.Application.Features.Events.GetEventById;

public sealed record GetEventByIdQuery(Guid Id) : IRequest<YouthEvent>;

public sealed class GetEventByIdHandler(ISupabaseGateway db)
    : IRequestHandler<GetEventByIdQuery, YouthEvent>
{
    public async Task<YouthEvent> Handle(GetEventByIdQuery q, CancellationToken ct)
    {
        var item = await db.SingleAsync<YouthEvent>("events",
            $"select=*&id=eq.{q.Id}", ct: ct);
        return item ?? throw new NotFoundException($"Event '{q.Id}' not found.");
    }
}
