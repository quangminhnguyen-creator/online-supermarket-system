using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Identity;

public sealed class RefreshToken : Entity
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime createdAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public bool IsActive => RevokedAtUtc == null && DateTime.UtcNow < ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc != null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var validTokenHash = Guard.Required(tokenHash, nameof(tokenHash));
        var now = DateTime.UtcNow;

        if (expiresAtUtc <= now)
        {
            throw new ArgumentException("ExpiresAtUtc must be in the future.", nameof(expiresAtUtc));
        }

        return new RefreshToken(
            Guid.NewGuid(),
            userId,
            validTokenHash,
            expiresAtUtc,
            now);
    }

    public void Revoke(DateTime revokedAtUtc, Guid? replacedByTokenId = null)
    {
        if (RevokedAtUtc != null)
        {
            throw new InvalidOperationException("Refresh token is already revoked.");
        }

        RevokedAtUtc = revokedAtUtc;
        ReplacedByTokenId = replacedByTokenId;
    }
}
