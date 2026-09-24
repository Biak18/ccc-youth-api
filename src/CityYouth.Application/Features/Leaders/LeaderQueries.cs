using CityYouth.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Leaders;

public sealed record ListLeadersQuery(bool IncludeHidden = false) : IRequest<IReadOnlyList<LeaderDto>>;

public sealed record GetLeaderByIdQuery(Guid Id) : IRequest<LeaderDto>;

public sealed class ListLeadersQueryHandler(IApplicationDbContext context)
    : IRequestHandler<ListLeadersQuery, IReadOnlyList<LeaderDto>>
{
    public async Task<IReadOnlyList<LeaderDto>> Handle(ListLeadersQuery request, CancellationToken cancellationToken)
    {
        var query = context.YouthLeaders.AsNoTracking().AsQueryable();

        if (!request.IncludeHidden)
        {
            query = query.Where(x => x.IsVisible);
        }

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new LeaderDto(
                x.Id, x.UserId, x.Name, x.RoleTitle, x.PhotoUrl, x.Bio, x.SortOrder, x.IsVisible))
            .ToListAsync(cancellationToken);
    }
}

public sealed class GetLeaderByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetLeaderByIdQuery, LeaderDto>
{
    public async Task<LeaderDto> Handle(GetLeaderByIdQuery request, CancellationToken cancellationToken)
    {
        var x = await context.YouthLeaders.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Leader not found.");

        return new LeaderDto(
            x.Id, x.UserId, x.Name, x.RoleTitle, x.PhotoUrl, x.Bio, x.SortOrder, x.IsVisible);
    }
}
