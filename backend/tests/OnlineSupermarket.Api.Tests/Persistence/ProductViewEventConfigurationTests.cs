using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Domain.Recommendations;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Tests.Persistence;

public sealed class ProductViewEventConfigurationTests
{
    [Fact]
    public void ProductViewEvent_IsMappedToProductViewEventsTable()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(ProductViewEvent));

        Assert.NotNull(entity);
        Assert.Equal("product_view_events", entity!.GetTableName());
    }

    [Fact]
    public void ProductViewEvent_HasMergeAndScoringIndexes()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProductViewEvent))!;

        Assert.Contains(entity.GetIndexes(),
            index => index.Properties.Select(p => p.Name).SequenceEqual(["UserId", "ViewedAtUtc"]));
        Assert.Contains(entity.GetIndexes(),
            index => index.Properties.Select(p => p.Name).SequenceEqual(["AnonymousSessionId", "ViewedAtUtc"]));
        Assert.Contains(entity.GetIndexes(),
            index => index.Properties.Select(p => p.Name).SequenceEqual(["ProductId", "ViewedAtUtc"]));
    }

    [Fact]
    public void ProductViewEvent_HasOptionalUserAndBranchForeignKeys()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProductViewEvent))!;

        var userFk = entity.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name).SequenceEqual(["UserId"]));
        Assert.False(userFk.IsRequired);

        var branchFk = entity.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name).SequenceEqual(["BranchId"]));
        Assert.False(branchFk.IsRequired);

        var productFk = entity.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name).SequenceEqual(["ProductId"]));
        Assert.True(productFk.IsRequired);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}