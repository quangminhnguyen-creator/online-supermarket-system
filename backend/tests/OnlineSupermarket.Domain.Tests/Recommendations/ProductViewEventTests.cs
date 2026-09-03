using System.Reflection;
using OnlineSupermarket.Domain.Recommendations;

namespace OnlineSupermarket.Domain.Tests.Recommendations;

public sealed class ProductViewEventTests
{
    [Fact]
    public void UserOwned_ViewEvent_HasUserIdAndNoAnonymousSession()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var view = ProductViewEvent.Create(productId, userId, null, null, DateTime.UtcNow);

        Assert.Equal(productId, view.ProductId);
        Assert.Equal(userId, view.UserId);
        Assert.Null(view.AnonymousSessionId);
    }

    [Fact]
    public void AnonymousOwned_ViewEvent_HasAnonymousSessionAndNoUserId()
    {
        var sessionId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var view = ProductViewEvent.Create(productId, null, sessionId, null, DateTime.UtcNow);

        Assert.Equal(productId, view.ProductId);
        Assert.Equal(sessionId, view.AnonymousSessionId);
        Assert.Null(view.UserId);
    }

    [Fact]
    public void Create_WithBranch_StoresOptionalBranchId()
    {
        var branchId = Guid.NewGuid();

        var view = ProductViewEvent.Create(
            Guid.NewGuid(), null, Guid.NewGuid(), branchId, DateTime.UtcNow);

        Assert.Equal(branchId, view.BranchId);
    }

    [Fact]
    public void Create_WithNoOwner_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ProductViewEvent.Create(Guid.NewGuid(), null, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithClaimedMergeOwner_AllowsBothUserAndAnonymousSession()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var view = ProductViewEvent.Create(
            Guid.NewGuid(), userId, sessionId, null, DateTime.UtcNow);

        Assert.Equal(userId, view.UserId);
        Assert.Equal(sessionId, view.AnonymousSessionId);
    }

    [Fact]
    public void Create_WithEmptyProductId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ProductViewEvent.Create(Guid.Empty, null, Guid.NewGuid(), null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ProductViewEvent.Create(Guid.NewGuid(), Guid.Empty, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithEmptyAnonymousSessionId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ProductViewEvent.Create(Guid.NewGuid(), null, Guid.Empty, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithNonUtcTimestamp_Throws()
    {
        var localNow = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() =>
            ProductViewEvent.Create(Guid.NewGuid(), null, Guid.NewGuid(), null, localNow));
    }

    [Fact]
    public void Entity_DoesNotExposeIpOrUserAgent()
    {
        var properties = typeof(ProductViewEvent)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name);

        Assert.DoesNotContain(properties, name => name.Contains("Ip", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, name => name.Contains("UserAgent", StringComparison.OrdinalIgnoreCase));
    }
}