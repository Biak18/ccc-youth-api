using CityYouth.Application.Abstractions;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using MediatR;

namespace CityYouth.Application.Features.Activities.GetActivityById;

public sealed record GetActivityByIdQuery(Guid Id) : IRequest<Activity>;

public sealed class GetActivityByIdHandler(ISupabaseGateway db)
    : IRequestHandler<GetActivityByIdQuery, Activity>
{
    public async Task<Activity> Handle(GetActivityByIdQuery q, CancellationToken ct)
    {
        var item = await db.SingleAsync<Activity>("activities",
            $"select=*&id=eq.{q.Id}", ct: ct);
        return item ?? throw new NotFoundException($"Activity '{q.Id}' not found.");
    }
}
