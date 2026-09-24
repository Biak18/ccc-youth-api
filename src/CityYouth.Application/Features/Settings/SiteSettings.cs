using CityYouth.Application.Abstractions;
using CityYouth.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Settings;

public sealed record GetSiteSettingsQuery : IRequest<SiteSettingsDto>;

public sealed record UpdateSiteSettingsCommand(
    string? ChurchName,
    string? YouthName,
    string? LogoUrl,
    string? HeroImageUrl,
    string? Tagline,
    string? Description,
    string? Address,
    string? Phone,
    string? Email,
    string? FacebookUrl,
    string? YoutubeUrl,
    string? InstagramUrl,
    string[]? HeroImages) : IRequest<SiteSettingsDto>;

public sealed class UpdateSiteSettingsCommandValidator : AbstractValidator<UpdateSiteSettingsCommand>
{
    public UpdateSiteSettingsCommandValidator()
    {
        _ = RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        _ = RuleFor(x => x.HeroImages!.Length).LessThanOrEqualTo(20).When(x => x.HeroImages is not null);
    }
}

public sealed class GetSiteSettingsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetSiteSettingsQuery, SiteSettingsDto>
{
    public async Task<SiteSettingsDto> Handle(GetSiteSettingsQuery request, CancellationToken cancellationToken)
    {
        var s = await context.SiteSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);

        if (s is null)
        {
            return new SiteSettingsDto(null, null, null, null, null, null, null, null, null, null, null, null, []);
        }

        return new SiteSettingsDto(
            s.ChurchName, s.YouthName, s.LogoUrl, s.HeroImageUrl, s.Tagline, s.Description,
            s.Address, s.Phone, s.Email, s.FacebookUrl, s.YoutubeUrl, s.InstagramUrl, s.HeroImages);
    }
}

public sealed class UpdateSiteSettingsCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateSiteSettingsCommand, SiteSettingsDto>
{
    public async Task<SiteSettingsDto> Handle(UpdateSiteSettingsCommand request, CancellationToken cancellationToken)
    {
        var s = await context.SiteSettings.FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);
        if (s is null)
        {
            s = new SiteSetting { Id = 1 };
            _ = context.SiteSettings.Add(s);
        }

        s.ChurchName = request.ChurchName;
        s.YouthName = request.YouthName;
        s.LogoUrl = request.LogoUrl;
        s.HeroImageUrl = request.HeroImageUrl;
        s.Tagline = request.Tagline;
        s.Description = request.Description;
        s.Address = request.Address;
        s.Phone = request.Phone;
        s.Email = request.Email;
        s.FacebookUrl = request.FacebookUrl;
        s.YoutubeUrl = request.YoutubeUrl;
        s.InstagramUrl = request.InstagramUrl;
        s.HeroImages = request.HeroImages ?? [];

        _ = await context.SaveChangesAsync(cancellationToken);

        return new SiteSettingsDto(
            s.ChurchName, s.YouthName, s.LogoUrl, s.HeroImageUrl, s.Tagline, s.Description,
            s.Address, s.Phone, s.Email, s.FacebookUrl, s.YoutubeUrl, s.InstagramUrl, s.HeroImages);
    }
}
