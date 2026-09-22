using CityYouth.Application.Abstractions;
using CityYouth.Domain.Common;
using CityYouth.Domain.Entities;
using MediatR;

namespace CityYouth.Application.Features.Users.ManageProfiles;

public sealed record ListProfilesQuery : IRequest<List<Profile>>;

public sealed class ListProfilesHandler(ISupabaseGateway db)
    : IRequestHandler<ListProfilesQuery, List<Profile>>
{
    public Task<List<Profile>> Handle(
        ListProfilesQuery _, CancellationToken ct) =>
        db.ListAsync<Profile>("profiles", "select=*&order=created_at.asc", ct: ct);
}

public sealed record SetProfileRoleCommand(Guid Id, string Role) : IRequest<Profile>;

public sealed class SetProfileRoleHandler(ISupabaseGateway db)
    : IRequestHandler<SetProfileRoleCommand, Profile>
{
    public async Task<Profile> Handle(SetProfileRoleCommand c, CancellationToken ct)
    {
        if (c.Role is not ("admin" or "leader"))
            throw new DomainException("Role must be admin or leader.");
        var profile = await db.SingleAsync<Profile>(
            "profiles", $"select=*&id=eq.{c.Id}", ct: ct)
            ?? throw new NotFoundException($"Profile '{c.Id}' not found.");
        profile.Role = c.Role;
        await db.UpdateAsync("profiles", "id", c.Id.ToString(),
            new { role = c.Role, updated_at = DateTimeOffset.UtcNow }, ct);
        return profile;
    }
}
