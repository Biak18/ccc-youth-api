namespace CityYouth.Application.Abstractions;

public sealed record AuthResult(string AccessToken, string RefreshToken, int ExpiresIn);

public interface IAuthClient
{
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken);

    Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task LogoutAsync(string accessToken, CancellationToken cancellationToken);
}
