using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Entities;

public sealed class Address : Entity
{
    private Address()
    {
    }

    private Address(
        Guid id,
        Guid userId,
        string recipientName,
        string phone,
        string street,
        string ward,
        string district,
        string city,
        string? postalCode,
        bool isDefault,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
        : base(id)
    {
        UserId = userId;
        RecipientName = recipientName;
        Phone = phone;
        Street = street;
        Ward = ward;
        District = district;
        City = city;
        PostalCode = postalCode;
        IsDefault = isDefault;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid UserId { get; private set; }
    public string RecipientName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Street { get; private set; } = string.Empty;
    public string Ward { get; private set; } = string.Empty;
    public string District { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string? PostalCode { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Address Create(
        Guid userId,
        string recipientName,
        string phone,
        string street,
        string ward,
        string district,
        string city,
        string? postalCode,
        bool isDefault = false)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
        var validUserId = userId;
        var validRecipientName = Guard.Required(recipientName, nameof(recipientName));
        var validPhone = Guard.Required(phone, nameof(phone));
        var validStreet = Guard.Required(street, nameof(street));
        var validWard = Guard.Required(ward, nameof(ward));
        var validDistrict = Guard.Required(district, nameof(district));
        var validCity = Guard.Required(city, nameof(city));
        var normalizedPostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim();
        var now = DateTime.UtcNow;

        return new Address(
            Guid.NewGuid(),
            validUserId,
            validRecipientName,
            validPhone,
            validStreet,
            validWard,
            validDistrict,
            validCity,
            normalizedPostalCode,
            isDefault,
            now,
            now);
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ClearDefault()
    {
        IsDefault = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Update(
        string recipientName,
        string phone,
        string street,
        string ward,
        string district,
        string city,
        string? postalCode)
    {
        RecipientName = Guard.Required(recipientName, nameof(recipientName));
        Phone = Guard.Required(phone, nameof(phone));
        Street = Guard.Required(street, nameof(street));
        Ward = Guard.Required(ward, nameof(ward));
        District = Guard.Required(district, nameof(district));
        City = Guard.Required(city, nameof(city));
        PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
