using System.Text.Json;
using CityYouth.Application.Abstractions;
using FluentValidation;
using MediatR;

namespace CityYouth.Application.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<JsonElement>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginHandler(ISupabaseGateway gateway) : IRequestHandler<LoginCommand, JsonElement>
{
    public Task<JsonElement> Handle(LoginCommand command, CancellationToken ct) =>
        gateway.LoginAsync(command.Email, command.Password, ct);
}
