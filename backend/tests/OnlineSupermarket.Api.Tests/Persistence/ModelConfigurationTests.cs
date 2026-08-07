using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Domain.Inventory;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Tests.Persistence;

public sealed class ModelConfigurationTests
{
    [Fact]
    public void BranchInventory_UsesCompositeUniqueIndex()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(BranchInventory));
        var index = entity!.GetIndexes().Single(candidate =>
            candidate.Properties.Select(property => property.Name).SequenceEqual([
                nameof(BranchInventory.BranchId),
                nameof(BranchInventory.ProductId),
            ]));

        Assert.True(index.IsUnique);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
