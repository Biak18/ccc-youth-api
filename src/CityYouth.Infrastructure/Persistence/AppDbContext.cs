using CityYouth.Application.Abstractions;
using CityYouth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Profile> Profiles => Set<Profile>();

    public DbSet<Activity> Activities => Set<Activity>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Media> Media => Set<Media>();

    public DbSet<Announcement> Announcements => Set<Announcement>();

    public DbSet<YouthLeader> YouthLeaders => Set<YouthLeader>();

    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Role/Status enums are persisted as text via value converters in the
        // entity configurations (schema is managed in Supabase).
        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
