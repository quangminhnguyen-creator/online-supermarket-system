using OnlineSupermarket.Domain.Identity;

namespace OnlineSupermarket.Domain.Tests.Identity;

public sealed class RefreshTokenTests
{
    [Fact]
    public void IssueRefreshToken_StoresHashAndExpiry()
    {
        var userId = Guid.NewGuid();
        var expiry = DateTime.UtcNow.AddDays(7);
        var token = RefreshToken.Issue(userId, "token_hash_sha256", expiry);

        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.Equal(userId, token.UserId);
        Assert.Equal("token_hash_sha256", token.TokenHash);
        Assert.Equal(expiry, token.ExpiresAtUtc);
        Assert.Null(token.RevokedAtUtc);
        Assert.Null(token.ReplacedByTokenId);
        Assert.True(token.IsActive);
        Assert.False(token.IsRevoked);
        Assert.False(token.IsExpired);
    }

    [Fact]
    public void IssueRefreshToken_WithPastExpiry_ThrowsArgumentException()
    {
        var userId = Guid.NewGuid();
        var pastExpiry = DateTime.UtcNow.AddMinutes(-5);

        Assert.Throws<ArgumentException>(() =>
            RefreshToken.Issue(userId, "token_hash_sha256", pastExpiry));
    }

    [Fact]
    public void Revoke_MarksTokenAsRevoked()
    {
        var userId = Guid.NewGuid();
        var expiry = DateTime.UtcNow.AddDays(7);
        var token = RefreshToken.Issue(userId, "token_hash_sha256", expiry);
        var replacedById = Guid.NewGuid();
        var revokedAt = DateTime.UtcNow;

        token.Revoke(revokedAt, replacedById);

        Assert.Equal(revokedAt, token.RevokedAtUtc);
        Assert.Equal(replacedById, token.ReplacedByTokenId);
        Assert.False(token.IsActive);
        Assert.True(token.IsRevoked);
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ThrowsInvalidOperationException()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(1));
        token.Revoke(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            token.Revoke(DateTime.UtcNow));
    }
}
