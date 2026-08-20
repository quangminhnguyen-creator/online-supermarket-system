using OnlineSupermarket.Domain.Identity;

namespace OnlineSupermarket.Domain.Tests.Identity;

public sealed class UserTests
{
    [Fact]
    public void CreateUser_WithValidInputs_CreatesActiveCustomer()
    {
        var user = User.Create("test@example.com", "hashed_password", "Nguyen Van A", "0901234567");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("hashed_password", user.PasswordHash);
        Assert.Equal("Nguyen Van A", user.FullName);
        Assert.Equal("0901234567", user.Phone);
        Assert.Equal(UserRole.Customer, user.Role);
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void CreateUser_NormalizesEmailToLowercase()
    {
        var user = User.Create("  User.Test@Example.COM  ", "hashed_password", "Nguyen Van A", null);

        Assert.Equal("user.test@example.com", user.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-email")]
    [InlineData("@nodomain.com")]
    [InlineData("noat.com@")]
    public void CreateUser_WithInvalidEmail_ThrowsArgumentException(string invalidEmail)
    {
        Assert.Throws<ArgumentException>(() =>
            User.Create(invalidEmail, "hashed_password", "Nguyen Van A", null));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateUser_WithBlankPasswordHash_ThrowsArgumentException(string blankPasswordHash)
    {
        Assert.Throws<ArgumentException>(() =>
            User.Create("valid@example.com", blankPasswordHash, "Nguyen Van A", null));
    }

    [Fact]
    public void ChangeStatus_UpdatesStatusAndTimestamp()
    {
        var user = User.Create("user@example.com", "hash", "User", null);
        user.ChangeStatus(UserStatus.Locked);

        Assert.Equal(UserStatus.Locked, user.Status);
    }

    [Fact]
    public void UpdateProfile_UpdatesNameAndPhone()
    {
        var user = User.Create("user@example.com", "hash", "User", null);
        user.UpdateProfile("New Name", "0987654321");

        Assert.Equal("New Name", user.FullName);
        Assert.Equal("0987654321", user.Phone);
    }
}
