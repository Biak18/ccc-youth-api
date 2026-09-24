using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Announcements;

public sealed record ListAnnouncementsQuery(
    string? Status,
    string? Search,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<AnnouncementDto>>;

public sealed record GetAnnouncementByIdQuery(Guid Id) : IRequest<AnnouncementDto>;

public sealed record GetAnnouncementBySlugQuery(string Slug) : IRequest<AnnouncementDto>;

public sealed class ListAnnouncementsQueryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<ListAnnouncementsQuery, PagedResult<AnnouncementDto>>
{
    public async Task<PagedResult<AnnouncementDto>> Handle(ListAnnouncementsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = context.Announcements.AsNoTracking().AsQueryable();

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

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(a => a.Title.Contains(s) || (a.Content != null && a.Content.Contains(s)));
        }

        query = query
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PublishedAt)
            .ThenByDescending(a => a.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AnnouncementDto(
                a.Id, a.Title, a.Slug, a.Content, a.CoverImageUrl, a.PublishedAt,
                a.IsPinned, a.Status.ToApiString(), a.CreatedBy, a.CreatedAt, a.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AnnouncementDto>(items, page, pageSize, total);
    }
}

public sealed class GetAnnouncementByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetAnnouncementByIdQuery, AnnouncementDto>
{
    public async Task<AnnouncementDto> Handle(GetAnnouncementByIdQuery request, CancellationToken cancellationToken)
    {
        var a = await context.Announcements.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Announcement not found.");

        return new AnnouncementDto(
            a.Id, a.Title, a.Slug, a.Content, a.CoverImageUrl, a.PublishedAt,
            a.IsPinned, a.Status.ToApiString(), a.CreatedBy, a.CreatedAt, a.UpdatedAt);
    }
}

public sealed class GetAnnouncementBySlugQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetAnnouncementBySlugQuery, AnnouncementDto>
{
    public async Task<AnnouncementDto> Handle(GetAnnouncementBySlugQuery request, CancellationToken cancellationToken)
    {
        var a = await context.Announcements.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == request.Slug, cancellationToken)
            ?? throw new KeyNotFoundException("Announcement not found.");

        return new AnnouncementDto(
            a.Id, a.Title, a.Slug, a.Content, a.CoverImageUrl, a.PublishedAt,
            a.IsPinned, a.Status.ToApiString(), a.CreatedBy, a.CreatedAt, a.UpdatedAt);
    }
}
