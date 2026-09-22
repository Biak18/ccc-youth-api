using CityYouth.Application.Abstractions;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using FluentValidation;
using MediatR;

namespace CityYouth.Application.Features.Activities.ListActivities;

public sealed record ListActivitiesQuery(
    string Status = "published",
    int? Year = null,
    string? Category = null,
    int Limit = 24,
    int Offset = 0) : IRequest<List<Activity>>;

public sealed class ListActivitiesValidator : AbstractValidator<ListActivitiesQuery>
{
    public ListActivitiesValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 200);
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
    }
}

public sealed class ListActivitiesHandler(ISupabaseGateway db)
    : IRequestHandler<ListActivitiesQuery, List<Activity>>
{
    public Task<List<Activity>> Handle(ListActivitiesQuery q, CancellationToken ct)
    {
        // published (current site) · all (published + archived, Memories) ·
        // any (everything incl. drafts — dashboard only, behind auth).
        var status = q.Status switch
        {
            "all" => "&status=in.(published,archived)",
            "any" => string.Empty,
            _ => "&status=eq.published",
        };
        var query = $"select=*&order=activity_date.desc{status}" +
                    $"&limit={q.Limit}&offset={q.Offset}";
        if (q.Year is { } y)
            query += $"&activity_date=gte.{y}-01-01&activity_date=lt.{y + 1}-01-01";
        if (!string.IsNullOrWhiteSpace(q.Category))
            query += $"&category=eq.{Uri.EscapeDataString(q.Category)}";
        return db.ListAsync<Activity>("activities", query, ct: ct);
    }
}
