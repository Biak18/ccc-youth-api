using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CityYouth.Infrastructure.Persistence.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events", "public");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.Title).HasColumnName("title").IsRequired();
        builder.Property(x => x.Slug).HasColumnName("slug").IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.CoverImageUrl).HasColumnName("cover_image_url");
        builder.Property(x => x.Location).HasColumnName("location");
        builder.Property(x => x.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(x => x.EndDate).HasColumnName("end_date");
        builder.Property(x => x.RegistrationUrl).HasColumnName("registration_url");
        builder.Property(x => x.ContactInformation).HasColumnName("contact_information");
        builder.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(20)
            .HasConversion(
                s => s.ToApiString(),
                s => ContentStatusExtensions.ParseApiString(s));
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
    }
}
