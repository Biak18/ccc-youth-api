using CityYouth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Profile> Profiles { get; }

    DbSet<Activity> Activities { get; }

    DbSet<Event> Events { get; }

    DbSet<Media> Media { get; }

    DbSet<Announcement> Announcements { get; }

    DbSet<YouthLeader> YouthLeaders { get; }

    DbSet<SiteSetting> SiteSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
