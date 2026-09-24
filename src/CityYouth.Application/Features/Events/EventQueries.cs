using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Events;

public sealed record ListEventsQuery(
    string? Filter,
    string? Status,
    string? Search,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<EventListDto>>;

public sealed record GetEventByIdQuery(Guid Id) : IRequest<EventDto>;

public sealed record GetEventBySlugQuery(string Slug) : IRequest<EventDto>;

public sealed class ListEventsQueryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<ListEventsQuery, PagedResult<EventListDto>>
{
    public async Task<PagedResult<EventListDto>> Handle(ListEventsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var now = DateTime.UtcNow;

        var query = context.Events.AsNoTracking().AsQueryable();

        var isAdmin = await AuthorizationHelper.IsAdminAsync(context, currentUser.UserId, cancellationToken);
        if (!isAdmin)
        {
            if (currentUser.UserId is null)
            {
                query = query.Where(e => e.Status == ContentStatus.Published);
            }
            else
            {
                var userId = currentUser.UserId.Value;
                query = query.Where(e =>
                    e.Status == ContentStatus.Published ||
                    e.Status == ContentStatus.Archived ||
                    e.CreatedBy == userId);
            }
        }
        else if (ContentStatusExtensions.TryParseApiString(request.Status, out var filterStatus))
        {
            query = query.Where(e => e.Status == filterStatus);
        }

        query = request.Filter?.ToLowerInvariant() switch
        {
            "upcoming" => query.Where(e => e.StartDate >= now).OrderBy(e => e.StartDate),
            "past" => query.Where(e => e.StartDate < now).OrderByDescending(e => e.StartDate),
            _ => query.OrderBy(e => e.StartDate),
        };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(e => e.Title.Contains(s) || (e.Description != null && e.Description.Contains(s)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EventListDto(
                e.Id, e.Title, e.Slug, e.CoverImageUrl, e.Location, e.StartDate, e.EndDate, e.Status.ToApiString()))
            .ToListAsync(cancellationToken);

        return new PagedResult<EventListDto>(items, page, pageSize, total);
    }
}

public sealed class GetEventByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetEventByIdQuery, EventDto>
{
    public async Task<EventDto> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var e = await context.Events.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Event not found.");

        return new EventDto(
            e.Id, e.Title, e.Slug, e.Description, e.CoverImageUrl, e.Location,
            e.StartDate, e.EndDate, e.RegistrationUrl, e.ContactInformation,
            e.Status.ToApiString(), e.CreatedBy, e.CreatedAt, e.UpdatedAt);
    }
}

public sealed class GetEventBySlugQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetEventBySlugQuery, EventDto>
{
    public async Task<EventDto> Handle(GetEventBySlugQuery request, CancellationToken cancellationToken)
    {
        var e = await context.Events.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == request.Slug, cancellationToken)
            ?? throw new KeyNotFoundException("Event not found.");

        return new EventDto(
            e.Id, e.Title, e.Slug, e.Description, e.CoverImageUrl, e.Location,
            e.StartDate, e.EndDate, e.RegistrationUrl, e.ContactInformation,
            e.Status.ToApiString(), e.CreatedBy, e.CreatedAt, e.UpdatedAt);
    }
}
