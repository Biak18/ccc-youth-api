using CityYouth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CityYouth.Infrastructure.Persistence.Configurations;

public sealed class YouthLeaderConfiguration : IEntityTypeConfiguration<YouthLeader>
{
    public void Configure(EntityTypeBuilder<YouthLeader> builder)
    {
        builder.ToTable("youth_leaders", "public");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.RoleTitle).HasColumnName("role_title");
        builder.Property(x => x.PhotoUrl).HasColumnName("photo_url");
        builder.Property(x => x.Bio).HasColumnName("bio");
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(x => x.IsVisible).HasColumnName("is_visible").HasDefaultValue(true);
    }
}
