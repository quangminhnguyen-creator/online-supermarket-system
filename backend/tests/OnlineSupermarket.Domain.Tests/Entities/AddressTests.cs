using OnlineSupermarket.Domain.Entities;

namespace OnlineSupermarket.Domain.Tests.Entities;

public sealed class AddressTests
{
    [Fact]
    public void Create_WithValidInputs_CreatesAddress()
    {
        var userId = Guid.NewGuid();
        var address = Address.Create(
            userId,
            "Nguyen Van A",
            "0912345678",
            "123 Main St",
            "Ward 1",
            "District 1",
            "Ho Chi Minh City",
            "700000");

        Assert.NotEqual(Guid.Empty, address.Id);
        Assert.Equal(userId, address.UserId);
        Assert.Equal("Nguyen Van A", address.RecipientName);
        Assert.Equal("0912345678", address.Phone);
        Assert.Equal("123 Main St", address.Street);
        Assert.Equal("Ward 1", address.Ward);
        Assert.Equal("District 1", address.District);
        Assert.Equal("Ho Chi Minh City", address.City);
        Assert.Equal("700000", address.PostalCode);
        Assert.False(address.IsDefault);
    }

    [Fact]
    public void Create_WithIsDefaultFlag_SetsDefaultFlag()
    {
        var address = Address.Create(
            Guid.NewGuid(),
            "Nguyen Van A",
            "0912345678",
            "123 Main St",
            "Ward 1",
            "District 1",
            "Ho Chi Minh City",
            null,
            isDefault: true);

        Assert.True(address.IsDefault);
    }

    [Fact]
    public void Create_WithBlankRecipientName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Address.Create(
                Guid.NewGuid(),
                "",
                "0912345678",
                "123 Main St",
                "Ward 1",
                "District 1",
                "Ho Chi Minh City",
                null));
    }

    [Fact]
    public void ClearDefault_SetsIsDefaultToFalse()
    {
        var address = Address.Create(
            Guid.NewGuid(),
            "Nguyen Van A",
            "0912345678",
            "123 Main St",
            "Ward 1",
            "District 1",
            "Ho Chi Minh City",
            null,
            isDefault: true);

        Assert.True(address.IsDefault);

        address.ClearDefault();

        Assert.False(address.IsDefault);
        Assert.True(address.UpdatedAtUtc >= DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public void SetAsDefault_UpdatesIsDefaultAndTimestamp()
    {
        var address = Address.Create(
            Guid.NewGuid(),
            "Nguyen Van A",
            "0912345678",
            "123 Main St",
            "Ward 1",
            "District 1",
            "Ho Chi Minh City",
            null);

        var beforeUpdate = address.UpdatedAtUtc;
        address.SetAsDefault();

        Assert.True(address.IsDefault);
        Assert.True(address.UpdatedAtUtc >= beforeUpdate);
    }

    [Fact]
    public void Update_UpdatesAllFieldsAndTimestamp()
    {
        var address = Address.Create(
            Guid.NewGuid(),
            "Nguyen Van A",
            "0912345678",
            "123 Main St",
            "Ward 1",
            "District 1",
            "Ho Chi Minh City",
            null);

        var beforeUpdate = address.UpdatedAtUtc;
        address.Update(
            "Nguyen Van B",
            "0987654321",
            "456 New St",
            "Ward 2",
            "District 2",
            "Ha Noi",
            "100000");

        Assert.Equal("Nguyen Van B", address.RecipientName);
        Assert.Equal("0987654321", address.Phone);
        Assert.Equal("456 New St", address.Street);
        Assert.Equal("Ward 2", address.Ward);
        Assert.Equal("District 2", address.District);
        Assert.Equal("Ha Noi", address.City);
        Assert.Equal("100000", address.PostalCode);
        Assert.True(address.UpdatedAtUtc >= beforeUpdate);
    }

    [Fact]
    public void Update_WithBlankStreet_ThrowsArgumentException()
    {
        var address = Address.Create(
            Guid.NewGuid(),
            "Nguyen Van A",
            "0912345678",
            "123 Main St",
            "Ward 1",
            "District 1",
            "Ho Chi Minh City",
            null);

        Assert.Throws<ArgumentException>(() =>
            address.Update(
                "Nguyen Van A",
                "0912345678",
                "",
                "Ward 1",
                "District 1",
                "Ho Chi Minh City",
                null));
    }
}
