using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using MediatR;

namespace CityYouth.Application.Features.Auth.GetMe;

public sealed record MeDto(string? Id, string? Email, string? Role);

public sealed record GetMeQuery : IRequest<MeDto>;

public sealed class GetMeHandler(
    ISupabaseGateway gateway, ICurrentUser user) : IRequestHandler<GetMeQuery, MeDto>
{
    public async Task<MeDto> Handle(GetMeQuery query, CancellationToken ct)
    {
        Authorization.RequireAuthenticated(user.UserId);
        var role = user.UserId is null
            ? null
            : await gateway.GetUserRoleAsync(user.UserId, ct);
        return new MeDto(user.UserId, user.Email, role);
    }
}
