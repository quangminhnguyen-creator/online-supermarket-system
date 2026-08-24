using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Identity;

public sealed class PasswordResetToken : Entity
{
    private PasswordResetToken()
    {
    }

    private PasswordResetToken(
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
        IsUsed = false;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public bool IsUsed { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsValid => !IsUsed && !IsExpired;

    public static PasswordResetToken Issue(Guid userId, string tokenHash, TimeSpan expiry)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var validTokenHash = Guard.Required(tokenHash, nameof(tokenHash));
        var now = DateTime.UtcNow;

        return new PasswordResetToken(
            Guid.NewGuid(),
            userId,
            validTokenHash,
            now.Add(expiry),
            now);
    }

    public void MarkAsUsed()
    {
        IsUsed = true;
    }
}
