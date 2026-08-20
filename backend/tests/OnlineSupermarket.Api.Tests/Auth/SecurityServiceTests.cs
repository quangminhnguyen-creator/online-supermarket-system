using Microsoft.Extensions.Options;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Infrastructure.Identity;

namespace OnlineSupermarket.Api.Tests.Auth;

public sealed class SecurityServiceTests
{
    [Fact]
    public void PasswordHasher_HashesAndVerifiesPasswordCorrectly()
    {
        var hasher = new PasswordHasher();
        const string password = "StrongPassword@123";

        var hash = hasher.HashPassword(password);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash);

        var verifyValid = hasher.VerifyPassword(hash, password);
        Assert.True(verifyValid);

        var verifyInvalid = hasher.VerifyPassword(hash, "WrongPassword@123");
        Assert.False(verifyInvalid);
    }

    [Fact]
    public void JwtTokenService_GeneratesAccessTokenAndRefreshToken()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            SecretKey = "super-secret-key-at-least-32-chars-long!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        });

        var tokenService = new JwtTokenService(jwtOptions);
        var user = User.Create("user@example.com", "hash", "Test User", "0900000000", UserRole.Customer);

        var accessToken = tokenService.GenerateAccessToken(user);
        Assert.NotEmpty(accessToken);

        var (rawToken, tokenHash, expiresAtUtc) = tokenService.GenerateRefreshToken();
        Assert.NotEmpty(rawToken);
        Assert.NotEmpty(tokenHash);
        Assert.True(expiresAtUtc > DateTime.UtcNow);

        var recomputedHash = tokenService.HashRefreshToken(rawToken);
        Assert.Equal(tokenHash, recomputedHash);
    }
}
