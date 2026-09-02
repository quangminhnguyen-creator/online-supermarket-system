using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Branches;

public sealed class Branch : Entity
{
    private Branch()
    {
    }

    public Branch(
        string name,
        string address,
        string? phone,
        decimal? latitude,
        decimal? longitude)
        : base(Guid.NewGuid())
    {
        Name = Guard.Required(name, nameof(name));
        Address = Guard.Required(address, nameof(address));
        Phone = phone?.Trim();
        Latitude = latitude;
        Longitude = longitude;
    }

    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(string name, string address, string? phone)
    {
        Name = Guard.Required(name, nameof(name));
        Address = Guard.Required(address, nameof(address));
        Phone = phone?.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
