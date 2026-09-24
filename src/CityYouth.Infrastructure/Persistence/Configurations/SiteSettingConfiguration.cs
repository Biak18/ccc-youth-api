using CityYouth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CityYouth.Infrastructure.Persistence.Configurations;

public sealed class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.ToTable("site_settings", "public");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ChurchName).HasColumnName("church_name");
        builder.Property(x => x.YouthName).HasColumnName("youth_name");
        builder.Property(x => x.LogoUrl).HasColumnName("logo_url");
        builder.Property(x => x.HeroImageUrl).HasColumnName("hero_image_url");
        builder.Property(x => x.Tagline).HasColumnName("tagline");
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.Address).HasColumnName("address");
        builder.Property(x => x.Phone).HasColumnName("phone");
        builder.Property(x => x.Email).HasColumnName("email");
        builder.Property(x => x.FacebookUrl).HasColumnName("facebook_url");
        builder.Property(x => x.YoutubeUrl).HasColumnName("youtube_url");
        builder.Property(x => x.InstagramUrl).HasColumnName("instagram_url");
        builder.Property(x => x.HeroImages).HasColumnName("hero_images").HasDefaultValueSql("'{}'::text[]");
    }
}
