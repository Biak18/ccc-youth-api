using CityYouth.Domain.Entities;
using MediatR;

namespace CityYouth.Application.Features.Leaders.ListLeaders;

public sealed record ListLeadersQuery(bool IncludeHidden = false) : IRequest<List<YouthLeader>>;

public sealed class ListLeadersHandler(ISupabaseGateway db)
    : IRequestHandler<ListLeadersQuery, List<YouthLeader>>
{
    public Task<List<YouthLeader>> Handle(ListLeadersQuery q, CancellationToken ct)
    {
        var query = "select=*&order=sort_order.asc";
        if (!q.IncludeHidden)
            query += "&is_visible=eq.true";
        return db.ListAsync<YouthLeader>("youth_leaders", query, ct: ct);
    }
}
