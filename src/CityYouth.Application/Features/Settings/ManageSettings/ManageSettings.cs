using CityYouth.Application.Abstractions;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using MediatR;

namespace CityYouth.Application.Features.Settings.UpdateSettings;

public sealed record UpdateSettingsCommand(
    string? ChurchName,
    string? Tagline,
    string? Description,
    string? Address,
    string? Phone,
    string? Email,
    string? FacebookUrl,
    string? YoutubeUrl,
    string? InstagramUrl,
    string[]? HeroImages,
    string? HeroImageUrl) : IRequest<SiteSettings>;

public sealed class UpdateSettingsHandler(ISupabaseGateway db)
    : IRequestHandler<UpdateSettingsCommand, SiteSettings>
{
    public async Task<SiteSettings> Handle(UpdateSettingsCommand c, CancellationToken ct)
    {
        var item = await db.SingleAsync<SiteSettings>("site_settings", "select=*&id=eq.1", ct: ct)
            ?? throw new NotFoundException("Site settings row (id = 1) not found.");

        static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
        item.ChurchName = Clean(c.ChurchName);
        item.Tagline = Clean(c.Tagline);
        item.Description = Clean(c.Description);
        item.Address = Clean(c.Address);
        item.Phone = Clean(c.Phone);
        item.Email = Clean(c.Email);
        item.FacebookUrl = Clean(c.FacebookUrl);
        item.YoutubeUrl = Clean(c.YoutubeUrl);
        item.InstagramUrl = Clean(c.InstagramUrl);
        item.HeroImages = (c.HeroImages ?? [])
            .Where(u => !string.IsNullOrWhiteSpace(u)).ToArray();
        item.HeroImageUrl = Clean(c.HeroImageUrl) ?? item.HeroImages.FirstOrDefault();

        await db.UpdateAsync("site_settings", "id", "1", item, ct);
        return item;
    }
}
