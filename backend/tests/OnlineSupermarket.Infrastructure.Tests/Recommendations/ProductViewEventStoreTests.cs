using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using OnlineSupermarket.Domain.Branches;
using OnlineSupermarket.Domain.Catalog;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Domain.Recommendations;
using OnlineSupermarket.Infrastructure.Persistence;
using OnlineSupermarket.Infrastructure.Recommendations;
using OnlineSupermarket.Infrastructure.Tests.Persistence;

namespace OnlineSupermarket.Infrastructure.Tests.Recommendations;

[Collection(MySqlInfrastructureCollection.Name)]
public sealed class ProductViewEventStoreTests : IAsyncLifetime
{
    private const string MasterConnectionString = "Server=127.0.0.1;Port=3306;Database=mysql;User=root;Password=password;";
    private const string TestDatabase = "online_supermarket_tests";
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=online_supermarket_tests;User=root;Password=password;";

    private static readonly DbContextOptions<AppDbContext> Options =
        new DbContextOptionsBuilder<AppDbContext>().UseMySQL(ConnectionString).Options;

    private static Guid ProductId;
    private static Guid SomeOtherUserId;

    public async Task InitializeAsync()
    {
        await using var master = new MySqlConnection(MasterConnectionString);
        await master.OpenAsync();

        await using (var drop = new MySqlCommand("DROP DATABASE IF EXISTS " + TestDatabase + ";", master))
        {
            await drop.ExecuteNonQueryAsync();
        }
        await using (var create = new MySqlCommand(
            "CREATE DATABASE " + TestDatabase + " CHARACTER SET utf8mb4;", master))
        {
            await create.ExecuteNonQueryAsync();
        }

        await using var db = new AppDbContext(Options);
        await db.Database.MigrateAsync();

        var category = new Category("View", "view");
        var brand = new Brand("View", "view");
        var branch = new Branch("View Branch", "1 Test Street", "0900000000", 10m, 106m);
        var product = new Product(category.Id, brand.Id, "SKU-V-1", "View Product", "view-product", "d", 10_000m, "cái", null);
        var otherUser = User.Create($"other_{Guid.NewGuid():N}@test.com", "hash", "Other", null);
        db.Categories.Add(category);
        db.Brands.Add(brand);
        db.Branches.Add(branch);
        db.Products.Add(product);
        db.Users.Add(otherUser);
        await db.SaveChangesAsync();

        ProductId = product.Id;
        SomeOtherUserId = otherUser.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<Guid> SeedEventsAsync(Guid sessionId)
    {
        await using var db = new AppDbContext(Options);

        db.ProductViewEvents.Add(ProductViewEvent.Create(ProductId, null, sessionId, null, DateTime.UtcNow));
        db.ProductViewEvents.Add(ProductViewEvent.Create(ProductId, null, sessionId, null, DateTime.UtcNow.AddSeconds(1)));
        db.ProductViewEvents.Add(ProductViewEvent.Create(ProductId, SomeOtherUserId, sessionId, null, DateTime.UtcNow.AddSeconds(2)));

        var userId = Guid.NewGuid();
        var user = User.Create($"merger_{Guid.NewGuid():N}@test.com", "hash", "Merger", null);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }

    [Fact]
    public async Task Merge_ClaimsOnlyUnownedRowsAndReturnsDeterministicCount()
    {
        var sessionId = Guid.NewGuid();
        var mergerUserId = await SeedEventsAsync(sessionId);

        await using var db = new AppDbContext(Options);
        var store = new ProductViewEventStore(db);

        var merged = await store.MergeAnonymousSessionAsync(sessionId, mergerUserId, CancellationToken.None);

        Assert.Equal(2, merged);

        var rows = await db.ProductViewEvents.AsNoTracking().ToListAsync();
        var sessionRows = rows.Where(r => r.AnonymousSessionId == sessionId).ToList();
        Assert.Equal(2, sessionRows.Count(r => r.UserId == mergerUserId));
        Assert.Equal(1, sessionRows.Count(r => r.UserId == SomeOtherUserId));
    }

    [Fact]
    public async Task Merge_RepeatedCall_ReturnsZero()
    {
        var sessionId = Guid.NewGuid();
        var mergerUserId = await SeedEventsAsync(sessionId);

        await using var db = new AppDbContext(Options);
        var store = new ProductViewEventStore(db);

        Assert.Equal(2, await store.MergeAnonymousSessionAsync(sessionId, mergerUserId, CancellationToken.None));
        Assert.Equal(0, await store.MergeAnonymousSessionAsync(sessionId, mergerUserId, CancellationToken.None));
    }

    [Fact]
    public async Task Merge_OwnedRows_AreNeverReassignedToAnotherUser()
    {
        var sessionId = Guid.NewGuid();
        await using (var seedDb = new AppDbContext(Options))
        {
            seedDb.ProductViewEvents.Add(
                ProductViewEvent.Create(ProductId, SomeOtherUserId, sessionId, null, DateTime.UtcNow));
            await seedDb.SaveChangesAsync();
        }

        await using var db = new AppDbContext(Options);
        var store = new ProductViewEventStore(db);
        var claimingUser = await CreateUserAsync(db);

        Assert.Equal(0, await store.MergeAnonymousSessionAsync(sessionId, claimingUser, CancellationToken.None));

        var owned = await db.ProductViewEvents.AsNoTracking()
            .SingleAsync(r => r.AnonymousSessionId == sessionId);
        Assert.Equal(SomeOtherUserId, owned.UserId);
    }

    [Fact]
    public async Task Merge_WithNoMatchingRows_ReturnsZero()
    {
        await using var db = new AppDbContext(Options);
        var store = new ProductViewEventStore(db);
        var claimingUser = await CreateUserAsync(db);

        Assert.Equal(0, await store.MergeAnonymousSessionAsync(
            Guid.NewGuid(), claimingUser, CancellationToken.None));
    }

    private static async Task<Guid> CreateUserAsync(AppDbContext db)
    {
        var user = User.Create($"claimer_{Guid.NewGuid():N}@test.com", "hash", "Claimer", null);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }
}