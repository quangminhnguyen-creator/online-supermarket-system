using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Tests.Persistence;

public sealed class IdentityConfigurationTests
{
    [Fact]
    public void User_HasUniqueIndexOnEmail_AndCompositeIndexOnStatusRole()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(User));
        Assert.NotNull(entity);
        Assert.Equal("users", entity.GetTableName());

        var emailIndex = entity.GetIndexes().Single(candidate =>
            candidate.Properties.Select(property => property.Name).SequenceEqual([nameof(User.Email)]));
        Assert.True(emailIndex.IsUnique);

        var statusRoleIndex = entity.GetIndexes().Single(candidate =>
            candidate.Properties.Select(property => property.Name).SequenceEqual([nameof(User.Status), nameof(User.Role)]));
        Assert.False(statusRoleIndex.IsUnique);
    }

    [Fact]
    public void RefreshToken_HasUniqueIndexOnTokenHash_AndCompositeIndexOnUserExpires()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(RefreshToken));
        Assert.NotNull(entity);
        Assert.Equal("refresh_tokens", entity.GetTableName());

        var tokenHashIndex = entity.GetIndexes().Single(candidate =>
            candidate.Properties.Select(property => property.Name).SequenceEqual([nameof(RefreshToken.TokenHash)]));
        Assert.True(tokenHashIndex.IsUnique);

        var userExpiresIndex = entity.GetIndexes().Single(candidate =>
            candidate.Properties.Select(property => property.Name).SequenceEqual([nameof(RefreshToken.UserId), nameof(RefreshToken.ExpiresAtUtc)]));
        Assert.False(userExpiresIndex.IsUnique);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
