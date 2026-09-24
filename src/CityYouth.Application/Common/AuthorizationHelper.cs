using CityYouth.Application.Abstractions;
using CityYouth.Domain.Common;
using CityYouth.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Common;

public static class AuthorizationHelper
{
    public static async Task<bool> IsAdminAsync(
        IApplicationDbContext context,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (userId is null)
        {
            return false;
        }

        return await context.Profiles.AsNoTracking()
            .AnyAsync(p => p.Id == userId && p.Role == UserRole.Admin, cancellationToken);
    }

    public static Guid RequireUser(Guid? userId)
    {
        if (userId is null)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        return userId.Value;
    }

    public static void RequireOwnerOrAdmin(Guid currentUserId, Guid? ownerId, bool isAdmin)
    {
        if (isAdmin)
        {
            return;
        }

        if (ownerId is null || ownerId != currentUserId)
        {
            throw new ForbiddenException("You can only modify your own content.");
        }
    }
}
