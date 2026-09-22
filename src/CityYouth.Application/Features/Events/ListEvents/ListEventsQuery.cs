using CityYouth.Application.Abstractions;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using FluentValidation;
using MediatR;

namespace CityYouth.Application.Features.Events.ListEvents;

public sealed record ListEventsQuery(string Filter = "upcoming", int Limit = 12)
    : IRequest<List<YouthEvent>>;

public sealed class ListEventsValidator : AbstractValidator<ListEventsQuery>
{
    public ListEventsValidator() => RuleFor(x => x.Limit).InclusiveBetween(1, 100);
}

public sealed class ListEventsHandler(ISupabaseGateway db)
    : IRequestHandler<ListEventsQuery, List<YouthEvent>>
{
    public Task<List<YouthEvent>> Handle(ListEventsQuery q, CancellationToken ct)
    {
        var now = Uri.EscapeDataString(DateTimeOffset.UtcNow.ToString("O"));
        var query = q.Filter.ToLowerInvariant() switch
        {
            "past" => $"select=*&status=eq.published&start_date=lt.{now}&order=start_date.desc",
            "all" => "select=*&status=eq.published&order=start_date.desc",
            _ => $"select=*&status=eq.published&start_date=gte.{now}&order=start_date.asc",
        };
        return db.ListAsync<YouthEvent>("events", $"{query}&limit={q.Limit}", ct: ct);
    }
}

public sealed record GetEventQuery(string Slug) : IRequest<YouthEvent>;

public sealed class GetEventHandler(ISupabaseGateway db)
    : IRequestHandler<GetEventQuery, YouthEvent>
{
    public async Task<YouthEvent> Handle(GetEventQuery q, CancellationToken ct)
    {
        var item = await db.SingleAsync<YouthEvent>("events",
            $"select=*&slug=eq.{Uri.EscapeDataString(q.Slug)}&status=eq.published", ct: ct);
        return item ?? throw new NotFoundException($"Event '{q.Slug}' not found.");
    }
}
