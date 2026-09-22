using System.Text.Json;
using CityYouth.Application.Abstractions;
using FluentValidation;
using MediatR;

namespace CityYouth.Application.Features.Auth.Refresh;

/// <summary>
/// Rotates an expired session: GoTrue returns a NEW access token AND a new
/// refresh token — the client must replace both stored values.
/// </summary>
public sealed record RefreshCommand(string RefreshToken) : IRequest<JsonElement>;

public sealed class RefreshValidator : AbstractValidator<RefreshCommand>
{
    public RefreshValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class RefreshHandler(ISupabaseGateway gateway)
    : IRequestHandler<RefreshCommand, JsonElement>
{
    public Task<JsonElement> Handle(RefreshCommand command, CancellationToken ct) =>
        gateway.RefreshAsync(command.RefreshToken, ct);
}
