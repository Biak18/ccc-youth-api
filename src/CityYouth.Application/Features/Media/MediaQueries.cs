using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Media;

public sealed record ListMediaQuery(
    Guid? ActivityId,
    string? Type,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<MediaDto>>;

public sealed record GetMediaByIdQuery(Guid Id) : IRequest<MediaDto>;

public sealed record ListActivityMediaQuery(Guid ActivityId) : IRequest<IReadOnlyList<MediaDto>>;

public sealed class ListMediaQueryHandler(IApplicationDbContext context)
    : IRequestHandler<ListMediaQuery, PagedResult<MediaDto>>
{
    public async Task<PagedResult<MediaDto>> Handle(ListMediaQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = context.Media.AsNoTracking().AsQueryable();

        if (request.ActivityId.HasValue)
        {
            query = query.Where(m => m.ActivityId == request.ActivityId);
        }

        if (!string.IsNullOrWhiteSpace(request.Type) && MediaTypes.IsValid(request.Type))
        {
            query = query.Where(m => m.Type == request.Type);
        }

        query = query.OrderBy(m => m.SortOrder).ThenBy(m => m.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MediaDto(
                m.Id, m.ActivityId, m.Type, m.Source, m.Url, m.ThumbnailUrl,
                m.Title, m.Description, m.SortOrder, m.UploadedBy, m.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<MediaDto>(items, page, pageSize, total);
    }
}

public sealed class GetMediaByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetMediaByIdQuery, MediaDto>
{
    public async Task<MediaDto> Handle(GetMediaByIdQuery request, CancellationToken cancellationToken)
    {
        var m = await context.Media.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Media not found.");

        return new MediaDto(
            m.Id, m.ActivityId, m.Type, m.Source, m.Url, m.ThumbnailUrl,
            m.Title, m.Description, m.SortOrder, m.UploadedBy, m.CreatedAt);
    }
}

public sealed class ListActivityMediaQueryHandler(IApplicationDbContext context)
    : IRequestHandler<ListActivityMediaQuery, IReadOnlyList<MediaDto>>
{
    public async Task<IReadOnlyList<MediaDto>> Handle(ListActivityMediaQuery request, CancellationToken cancellationToken)
    {
        return await context.Media.AsNoTracking()
            .Where(m => m.ActivityId == request.ActivityId)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .Select(m => new MediaDto(
                m.Id, m.ActivityId, m.Type, m.Source, m.Url, m.ThumbnailUrl,
                m.Title, m.Description, m.SortOrder, m.UploadedBy, m.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
