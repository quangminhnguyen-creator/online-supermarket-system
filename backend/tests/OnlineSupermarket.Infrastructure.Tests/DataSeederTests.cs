using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Domain.Orders;
using OnlineSupermarket.Infrastructure.Identity;
using OnlineSupermarket.Infrastructure.Persistence;
using OnlineSupermarket.Infrastructure.Persistence.SeedData;

namespace OnlineSupermarket.Infrastructure.Tests;

public sealed class DataSeederTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task SeedAllAsync_SeedsAllEntitiesCorrectly()
    {
        using var context = CreateInMemoryDbContext();
        var hasher = new PasswordHasher();

        await DataSeeder.SeedAllAsync(context, hasher);

        // 1. Branches
        var branches = await context.Branches.ToListAsync();
        Assert.Equal(3, branches.Count);
        Assert.Contains(branches, b => b.Name == "AptechMart Quận 1");
        Assert.Contains(branches, b => b.Name == "AptechMart Quận 3");
        Assert.Contains(branches, b => b.Name == "AptechMart Bình Thạnh");

        // 2. Categories
        var categories = await context.Categories.ToListAsync();
        Assert.Equal(8, categories.Count);
        Assert.Contains(categories, c => c.Slug == "dien-thoai-tablet");
        Assert.Contains(categories, c => c.Slug == "laptop-may-tinh");
        Assert.Contains(categories, c => c.Slug == "tv-man-hinh");
        Assert.Contains(categories, c => c.Slug == "thiet-bi-gia-dung");
        Assert.Contains(categories, c => c.Slug == "am-thanh-loa");
        Assert.Contains(categories, c => c.Slug == "phu-kien");
        Assert.Contains(categories, c => c.Slug == "game-gaming");
        Assert.Contains(categories, c => c.Slug == "camera-an-ninh");

        // 3. Brands
        var brands = await context.Brands.ToListAsync();
        Assert.Equal(10, brands.Count);
        Assert.Contains(brands, b => b.Slug == "samsung");
        Assert.Contains(brands, b => b.Slug == "apple");
        Assert.Contains(brands, b => b.Slug == "sony");
        Assert.Contains(brands, b => b.Slug == "lg");
        Assert.Contains(brands, b => b.Slug == "dell");
        Assert.Contains(brands, b => b.Slug == "asus");
        Assert.Contains(brands, b => b.Slug == "xiaomi");
        Assert.Contains(brands, b => b.Slug == "panasonic");
        Assert.Contains(brands, b => b.Slug == "jbl");
        Assert.Contains(brands, b => b.Slug == "canon");

        // 4. Products
        var products = await context.Products.ToListAsync();
        Assert.True(products.Count >= 20);
        Assert.Contains(products, p => p.Sku == "DT-SAM-001");
        Assert.Contains(products, p => p.Sku == "DT-APP-001");
        Assert.Contains(products, p => p.Sku == "LT-DEL-001");
        Assert.Contains(products, p => p.Sku == "LT-APP-001");
        Assert.Contains(products, p => p.Sku == "TV-SAM-001");
        Assert.Contains(products, p => p.Sku == "GD-PAN-001");
        Assert.Contains(products, p => p.Sku == "AT-JBL-001");

        // 5. Branch Inventories
        var inventories = await context.BranchInventories.ToListAsync();
        Assert.Equal(branches.Count * products.Count, inventories.Count);

        // 6. Users
        var users = await context.Users.ToListAsync();
        Assert.Equal(4, users.Count);
        var user1 = users.Single(u => u.Email == "user1@test.com");
        Assert.Equal("Nguyen Van An", user1.FullName);
        Assert.Equal(UserRole.Customer, user1.Role);
        Assert.True(hasher.VerifyPassword(user1.PasswordHash, "Test@123"));

        var admin = users.Single(u => u.Email == "admin@test.com");
        Assert.Equal("Admin User", admin.FullName);
        Assert.Equal(UserRole.Admin, admin.Role);

        // 7. Addresses
        var addresses = await context.Addresses.ToListAsync();
        Assert.Equal(4, addresses.Count);
        Assert.Equal(2, addresses.Count(a => a.UserId == user1.Id));

        // 8. Carts
        var carts = await context.Carts.Include(c => c.Items).ToListAsync();
        Assert.Single(carts);
        var cart = carts.First();
        Assert.Equal(3, cart.Items.Count);

        // 9. Orders
        var orders = await context.Orders.Include(o => o.Items).ToListAsync();
        Assert.True(orders.Count >= 4);
        Assert.Contains(orders, o => o.Status == OrderStatus.Completed);
        Assert.Contains(orders, o => o.Status == OrderStatus.Shipped);
        Assert.Contains(orders, o => o.Status == OrderStatus.Preparing);
    }

    [Fact]
    public async Task SeedAllAsync_IsIdempotent()
    {
        using var context = CreateInMemoryDbContext();
        var hasher = new PasswordHasher();

        await DataSeeder.SeedAllAsync(context, hasher);
        var branchCountFirst = await context.Branches.CountAsync();
        var productCountFirst = await context.Products.CountAsync();
        var userCountFirst = await context.Users.CountAsync();

        // Run seed again
        await DataSeeder.SeedAllAsync(context, hasher);

        Assert.Equal(branchCountFirst, await context.Branches.CountAsync());
        Assert.Equal(productCountFirst, await context.Products.CountAsync());
        Assert.Equal(userCountFirst, await context.Users.CountAsync());
    }

    [Fact]
    public void CatalogSeedTaxonomy_DefinesRootsLeavesAndUncategorized()
    {
        Assert.Equal(23, CatalogSeedTaxonomy.Categories.Count);
        var roots = CatalogSeedTaxonomy.Categories.Where(c => c.ParentSlug is null).ToList();
        var navigationParents = roots.Where(root =>
            CatalogSeedTaxonomy.Categories.Any(child => child.ParentSlug == root.Slug));

        Assert.Equal(9, roots.Count);
        Assert.Equal(8, navigationParents.Count());
        Assert.Equal(14, CatalogSeedTaxonomy.Categories.Count(c => c.ParentSlug is not null));
        Assert.Contains(CatalogSeedTaxonomy.Categories,
            c => c.Slug == "man-hinh-may-tinh" && c.ParentSlug == "tv-man-hinh");
        Assert.Contains(CatalogSeedTaxonomy.Categories,
            c => c.Slug == "uncategorized" && c.ParentSlug is null);
    }

    [Theory]
    [InlineData("TV-SAM-001", "tivi")]
    [InlineData("MH-SAM-001", "man-hinh-may-tinh")]
    [InlineData("AT-SON-001", "tai-nghe")]
    [InlineData("AT-JBL-002", "loa")]
    [InlineData("UNKNOWN-SKU", "uncategorized")]
    [InlineData(null, "uncategorized")]
    public void ResolveProductCategorySlug_ReturnsExpectedLeafOrFallback(
        string? sku,
        string expectedSlug)
    {
        Assert.Equal(expectedSlug, CatalogSeedTaxonomy.ResolveProductCategorySlug(sku));
    }

    [Fact]
    public void ResolveProductCategoryId_WithUnmappedSku_ReturnsUncategorizedId()
    {
        var uncategorizedId = Guid.NewGuid();
        var categoryIds = new Dictionary<string, Guid>
        {
            ["dien-thoai"] = Guid.NewGuid(),
            ["uncategorized"] = uncategorizedId,
        };

        var result = CatalogSeedTaxonomy.ResolveProductCategoryId("UNKNOWN-SKU", categoryIds);

        Assert.Equal(uncategorizedId, result);
    }
}
