using OnlineSupermarket.Domain.Identity;

namespace OnlineSupermarket.Infrastructure.Identity;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    (string RawToken, string TokenHash, DateTime ExpiresAtUtc) GenerateRefreshToken();
    string HashRefreshToken(string rawToken);
    int AccessTokenExpirationMinutes { get; }
}
