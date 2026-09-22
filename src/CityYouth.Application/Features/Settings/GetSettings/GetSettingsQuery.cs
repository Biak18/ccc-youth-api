using CityYouth.Application.Abstractions;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using MediatR;

namespace CityYouth.Application.Features.Settings.GetSettings;

public sealed record GetSettingsQuery : IRequest<SiteSettings>;

public sealed class GetSettingsHandler(ISupabaseGateway db)
    : IRequestHandler<GetSettingsQuery, SiteSettings>
{
    public async Task<SiteSettings> Handle(GetSettingsQuery _, CancellationToken ct)
    {
        var item = await db.SingleAsync<SiteSettings>("site_settings", "select=*&id=eq.1", ct: ct);
        return item ?? throw new NotFoundException("Site settings row (id = 1) not found.");
    }
}
