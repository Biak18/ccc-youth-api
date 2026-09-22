using CityYouth.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace CityYouth.Api.Authorization;

public sealed class AdminRequirement : IAuthorizationRequirement;

public sealed class AdminAuthorizationHandler(
    ICurrentUser currentUser, IRoleChecker roles) : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        if (currentUser.UserId is not null
            && await roles.IsAdminAsync(currentUser.UserId))
            context.Succeed(requirement);
    }
}
