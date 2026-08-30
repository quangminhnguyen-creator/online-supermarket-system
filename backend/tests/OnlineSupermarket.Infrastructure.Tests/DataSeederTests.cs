using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OnlineSupermarket.Domain.Catalog;
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
        Assert.Equal(23, categories.Count);

        var bySlug = categories.ToDictionary(c => c.Slug);
        Assert.Equal(bySlug["tv-man-hinh"].Id, bySlug["tivi"].ParentCategoryId);
        Assert.Equal(bySlug["tv-man-hinh"].Id, bySlug["man-hinh-may-tinh"].ParentCategoryId);
        Assert.Equal(bySlug["am-thanh-loa"].Id, bySlug["tai-nghe"].ParentCategoryId);
        Assert.Equal(bySlug["am-thanh-loa"].Id, bySlug["loa"].ParentCategoryId);
        Assert.Null(bySlug["uncategorized"].ParentCategoryId);

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

        // Every seeded product must live in a leaf category matching its SKU mapping.
        var parentIds = categories
            .Where(parent => categories.Any(child => child.ParentCategoryId == parent.Id))
            .Select(parent => parent.Id)
            .ToHashSet();
        Assert.DoesNotContain(products, product => parentIds.Contains(product.CategoryId));
        foreach (var product in products)
        {
            var expectedSlug = CatalogSeedTaxonomy.ResolveProductCategorySlug(product.Sku);
            Assert.Equal(bySlug[expectedSlug].Id, product.CategoryId);
        }

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

    [Fact]
    public async Task UnmappedSku_IsPersistedInUncategorized()
    {
        using var context = CreateInMemoryDbContext();
        await DataSeeder.SeedCategoriesAsync(context);
        await DataSeeder.SeedBrandsAsync(context);

        var categoryIds = await context.Categories.ToDictionaryAsync(c => c.Slug, c => c.Id);
        var appleBrandId = await context.Brands
            .Where(brand => brand.Slug == "apple")
            .Select(brand => brand.Id)
            .SingleAsync();
        var resolvedCategoryId = CatalogSeedTaxonomy.ResolveProductCategoryId(
            "UNMAPPED-SKU",
            categoryIds);
        var product = new Product(
            resolvedCategoryId,
            appleBrandId,
            "UNMAPPED-SKU",
            "Sản phẩm chờ phân loại",
            "san-pham-cho-phan-loai",
            null,
            100_000m,
            "cái",
            null);

        context.Products.Add(product);
        await context.SaveChangesAsync();

        Assert.Equal(categoryIds["uncategorized"], product.CategoryId);
        Assert.Equal(product.Id, (await context.Products.SingleAsync()).Id);
    }

    [Fact]
    public async Task Reconcile_MovesSeededProductsBackToLeafWithoutChangingIds()
    {
        using var context = CreateInMemoryDbContext();
        var hasher = new PasswordHasher();

        await DataSeeder.SeedAllAsync(context, hasher);
        var originalIds = (await context.Products.ToListAsync()).ToDictionary(p => p.Sku, p => p.Id);

        var categoriesBySlug = await context.Categories.ToDictionaryAsync(c => c.Slug, c => c.Id);
        var products = await context.Products
            .Where(p =>
                p.Sku == "TV-SAM-001" || p.Sku == "MH-SAM-001" ||
                p.Sku == "AT-SON-001" || p.Sku == "AT-JBL-002")
            .ToListAsync();

        // Simulate legacy rows still pointing at navigation parents.
        products.Single(p => p.Sku == "TV-SAM-001").ChangeCategory(categoriesBySlug["tv-man-hinh"]);
        products.Single(p => p.Sku == "MH-SAM-001").ChangeCategory(categoriesBySlug["tv-man-hinh"]);
        products.Single(p => p.Sku == "AT-SON-001").ChangeCategory(categoriesBySlug["am-thanh-loa"]);
        products.Single(p => p.Sku == "AT-JBL-002").ChangeCategory(categoriesBySlug["am-thanh-loa"]);
        await context.SaveChangesAsync();

        await DataSeeder.SeedAllAsync(context, hasher);

        var bySlug = await context.Categories.ToDictionaryAsync(c => c.Slug, c => c.Id);
        var productsAfter = (await context.Products.ToListAsync()).ToDictionary(p => p.Sku);

        Assert.Equal(originalIds["TV-SAM-001"], productsAfter["TV-SAM-001"].Id);
        Assert.Equal(bySlug["tivi"], productsAfter["TV-SAM-001"].CategoryId);
        Assert.Equal(bySlug["man-hinh-may-tinh"], productsAfter["MH-SAM-001"].CategoryId);
        Assert.Equal(bySlug["tai-nghe"], productsAfter["AT-SON-001"].CategoryId);
        Assert.Equal(bySlug["loa"], productsAfter["AT-JBL-002"].CategoryId);
    }

    [Fact]
    public async Task Reconcile_MatchesSkuCaseInsensitively()
    {
        using var context = CreateInMemoryDbContext();
        var hasher = new PasswordHasher();

        await DataSeeder.SeedAllAsync(context, hasher);
        var tv = await context.Products.SingleAsync(p => p.Sku == "TV-SAM-001");
        context.Entry(tv).Property(p => p.Sku).CurrentValue = "tv-sam-001";
        tv.ChangeCategory((await context.Categories.SingleAsync(c => c.Slug == "tv-man-hinh")).Id);
        await context.SaveChangesAsync();

        await DataSeeder.SeedAllAsync(context, hasher);

        var bySlug = await context.Categories.ToDictionaryAsync(c => c.Slug, c => c.Id);
        var reconciled = await context.Products.SingleAsync(p => p.Sku == "tv-sam-001");
        Assert.Equal(bySlug["tivi"], reconciled.CategoryId);
    }
}
