using CityYouth.Application.Abstractions;
using CityYouth.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Auth;

public sealed record LoginCommand(string Email, string Password)
    : IRequest<LoginResponse>;

public sealed record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        _ = RuleFor(x => x.Email).NotEmpty().EmailAddress();
        _ = RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public sealed class LoginCommandHandler(IAuthClient authClient)
    : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await authClient.LoginAsync(request.Email, request.Password, cancellationToken);
        return new LoginResponse(result.AccessToken, result.RefreshToken, result.ExpiresIn);
    }
}

public sealed record RefreshTokenCommand(string RefreshToken)
    : IRequest<LoginResponse>;

public sealed class RefreshTokenCommandHandler(IAuthClient authClient)
    : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = await authClient.RefreshAsync(request.RefreshToken, cancellationToken);
        return new LoginResponse(result.AccessToken, result.RefreshToken, result.ExpiresIn);
    }
}

public sealed record LogoutCommand(string AccessToken) : IRequest;

public sealed class LogoutCommandHandler(IAuthClient authClient)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await authClient.LogoutAsync(request.AccessToken, cancellationToken);
    }
}

public sealed record MeResponse(
    Guid Id,
    string? Email,
    string? DisplayName,
    string Role);

public sealed record GetMeQuery : IRequest<MeResponse>;

public sealed class GetMeQueryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<GetMeQuery, MeResponse>
{
    public async Task<MeResponse> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var profile = await context.Profiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);

        return new MeResponse(
            userId,
            profile?.Email ?? currentUser.Email,
            profile?.DisplayName,
            profile is null ? "leader" : profile.Role.ToApiString());
    }
}
