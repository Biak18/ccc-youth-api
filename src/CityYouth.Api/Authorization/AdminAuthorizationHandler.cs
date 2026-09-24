using CityYouth.Application.Abstractions;
using CityYouth.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Api.Authorization;

public sealed class AdminAuthorizationHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authorizationContext,
        AdminRequirement requirement)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return;
        }

        var isAdmin = await context.Profiles.AsNoTracking()
            .AnyAsync(p => p.Id == userId && p.Role == UserRole.Admin);

        if (isAdmin)
        {
            authorizationContext.Succeed(requirement);
        }
    }
}
