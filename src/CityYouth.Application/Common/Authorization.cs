using CityYouth.Domain.Common;

namespace CityYouth.Application.Common;

/// <summary>Ownership rule (§7, ARCHITECTURE.md): leaders manage their own
/// content (created_by), admins manage everything.</summary>
public static class Authorization
{
    public static void EnsureCanEdit(string? createdBy, string? actorId, bool isAdmin)
    {
        if (isAdmin) return;
        if (actorId is not null && createdBy == actorId) return;
        throw new ForbiddenException("You can only manage your own content.");
    }

    public static void RequireAuthenticated(string? actorId)
    {
        if (actorId is null)
            throw new ForbiddenException("Authentication required.");
    }
}
