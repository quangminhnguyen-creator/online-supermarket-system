using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Identity;

public sealed class User : Entity
{
    private User()
    {
    }

    private User(
        Guid id,
        string email,
        string passwordHash,
        string fullName,
        string? phone,
        UserRole role,
        UserStatus status,
        DateTime createdAtUtc)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        Phone = phone;
        Role = role;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static User Create(
        string email,
        string passwordHash,
        string fullName,
        string? phone,
        UserRole role = UserRole.Customer)
    {
        var normalizedEmail = Guard.Required(email, nameof(email)).ToLowerInvariant();
        if (!normalizedEmail.Contains('@') || normalizedEmail.StartsWith('@') || normalizedEmail.EndsWith('@'))
        {
            throw new ArgumentException("Email must be a valid email address.", nameof(email));
        }

        var validPasswordHash = Guard.Required(passwordHash, nameof(passwordHash));
        var validFullName = Guard.Required(fullName, nameof(fullName));
        var normalizedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        return new User(
            Guid.NewGuid(),
            normalizedEmail,
            validPasswordHash,
            validFullName,
            normalizedPhone,
            role,
            UserStatus.Active,
            DateTime.UtcNow);
    }

    public void ChangeStatus(UserStatus newStatus)
    {
        Status = newStatus;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateProfile(string fullName, string? phone)
    {
        FullName = Guard.Required(fullName, nameof(fullName));
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = Guard.Required(newPasswordHash, nameof(newPasswordHash));
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
