using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Activities;

public sealed record ListActivitiesQuery(
    string? Status,
    int? Year,
    string? Category,
    string? Search,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ActivityListDto>>;

public sealed record GetActivityByIdQuery(Guid Id) : IRequest<ActivityDto>;

public sealed record GetActivityBySlugQuery(string Slug) : IRequest<ActivityDto>;

public sealed class ListActivitiesQueryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<ListActivitiesQuery, PagedResult<ActivityListDto>>
{
    public async Task<PagedResult<ActivityListDto>> Handle(ListActivitiesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = context.Activities.AsNoTracking().AsQueryable();

        var isAdmin = await AuthorizationHelper.IsAdminAsync(context, currentUser.UserId, cancellationToken);
        if (!isAdmin)
        {
            if (currentUser.UserId is null)
            {
                query = query.Where(a => a.Status == ContentStatus.Published);
            }
            else
            {
                var userId = currentUser.UserId.Value;
                query = query.Where(a =>
                    a.Status == ContentStatus.Published ||
                    a.Status == ContentStatus.Archived ||
                    a.CreatedBy == userId);
            }
        }
        else
        {
            // Admins without a status filter see everything.
        }

        if (ContentStatusExtensions.TryParseApiString(request.Status, out var filterStatus))
        {
            query = query.Where(a => a.Status == filterStatus);
        }

        if (request.Year.HasValue)
        {
            query = query.Where(a => a.ActivityDate.Year == request.Year.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(a => a.Category == request.Category);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(a => a.Title.Contains(s) || (a.Description != null && a.Description.Contains(s)));
        }

        query = query.OrderByDescending(a => a.ActivityDate).ThenByDescending(a => a.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ActivityListDto(
                a.Id, a.Title, a.Slug, a.CoverImageUrl, a.ActivityDate, a.Category, a.Status.ToApiString()))
            .ToListAsync(cancellationToken);

        return new PagedResult<ActivityListDto>(items, page, pageSize, total);
    }
}

public sealed class GetActivityByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetActivityByIdQuery, ActivityDto>
{
    public async Task<ActivityDto> Handle(GetActivityByIdQuery request, CancellationToken cancellationToken)
    {
        var a = await context.Activities.AsNoTracking()
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Activity not found.");

        return new ActivityDto(
            a.Id, a.Title, a.Slug, a.Description, a.ActivityDate, a.Location,
            a.CoverImageUrl, a.Status.ToApiString(), a.Category, a.CreatedBy, a.CreatedAt, a.UpdatedAt, a.Media.Count);
    }
}

public sealed class GetActivityBySlugQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetActivityBySlugQuery, ActivityDto>
{
    public async Task<ActivityDto> Handle(GetActivityBySlugQuery request, CancellationToken cancellationToken)
    {
        var a = await context.Activities.AsNoTracking()
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Slug == request.Slug, cancellationToken)
            ?? throw new KeyNotFoundException("Activity not found.");

        return new ActivityDto(
            a.Id, a.Title, a.Slug, a.Description, a.ActivityDate, a.Location,
            a.CoverImageUrl, a.Status.ToApiString(), a.Category, a.CreatedBy, a.CreatedAt, a.UpdatedAt, a.Media.Count);
    }
}
