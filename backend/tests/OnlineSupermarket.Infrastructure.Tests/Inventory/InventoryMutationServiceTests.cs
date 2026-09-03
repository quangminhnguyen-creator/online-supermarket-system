using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Domain.Branches;
using OnlineSupermarket.Domain.Catalog;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Domain.Inventory;
using OnlineSupermarket.Infrastructure.Inventory;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Infrastructure.Tests.Inventory;

public sealed class InventoryMutationServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly InventoryMutationService _service;
    private Guid _lowId;
    private Guid _highId;
    private readonly Guid _orderId = Guid.NewGuid();
    private Guid _userId;

    public InventoryMutationServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new InventoryMutationService(_db, TimeProvider.System);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task SeedInventoriesAsync()
    {
        var branch = new Branch("Branch", "Address", "0900000000", null, null);
        _db.Branches.Add(branch);
        var category = new Category("Category", "cat");
        var brand = new Brand("Brand", "brand");
        _db.Categories.Add(category);
        _db.Brands.Add(brand);
        var user = User.Create($"actor_{Guid.NewGuid():N}@example.com", "hash", "Actor", "0911111111");
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        _userId = user.Id;

        var productLow = new Product(category.Id, brand.Id, "SKU-1", "Product 1", "product-1", "desc", 10m, "cái", null);
        var productHigh = new Product(category.Id, brand.Id, "SKU-2", "Product 2", "product-2", "desc", 10m, "cái", null);
        _db.Products.AddRange(productLow, productHigh);
        await _db.SaveChangesAsync();

        _db.BranchInventories.AddRange(
            BranchInventory.Create(branch.Id, productLow.Id, 10m, 100, 20),
            BranchInventory.Create(branch.Id, productHigh.Id, 10m, 100, 20));
        await _db.SaveChangesAsync();

        var rows = await _db.BranchInventories.OrderBy(bi => bi.Id).ToListAsync();
        _lowId = rows[0].Id;
        _highId = rows[1].Id;
    }

    [Fact]
    public async Task ApplyBatchAsync_LoadsByAscendingInventoryId_AndAddsLedgerRows()
    {
        await SeedInventoriesAsync();
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var commands = new[]
        {
            InventoryMutationCommand.Reserve(_highId, 2, _orderId, _userId),
            InventoryMutationCommand.Reserve(_lowId, 1, _orderId, _userId),
        };

        await _service.ApplyBatchAsync(commands, CancellationToken.None);
        await _db.SaveChangesAsync();

        var ledgerOrder = _db.ChangeTracker.Entries<InventoryTransaction>()
            .Select(entry => entry.Entity.BranchInventoryId)
            .ToArray();
        Assert.Equal(new[] { _lowId, _highId }, ledgerOrder);
        Assert.Equal(2, await _db.InventoryTransactions.CountAsync());
    }

    [Fact]
    public async Task ApplyBatchAsync_WithoutTransaction_Throws()
    {
        await SeedInventoriesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ApplyBatchAsync(
                new[] { InventoryMutationCommand.Reserve(_lowId, 1, _orderId, _userId) },
                CancellationToken.None));
    }

    [Fact]
    public async Task ApplyBatchAsync_WithSameOperationKey_AppliesOnce()
    {
        await SeedInventoriesAsync();
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var reserve = InventoryMutationCommand.Reserve(_lowId, 1, _orderId, _userId);

        await _service.ApplyBatchAsync(new[] { reserve }, CancellationToken.None);
        await _db.SaveChangesAsync();
        await _service.ApplyBatchAsync(new[] { reserve }, CancellationToken.None);
        await _db.SaveChangesAsync();

        Assert.Equal(1, await _db.InventoryTransactions.CountAsync());
        var inventory = await _db.BranchInventories.SingleAsync(bi => bi.Id == _lowId);
        Assert.Equal(1, inventory.ReservedQuantity);
    }

    [Fact]
    public async Task ApplyBatchAsync_WithMismatchedOperationKey_ThrowsConflict()
    {
        await SeedInventoriesAsync();
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var first = new InventoryMutationCommand(
            _lowId, InventoryTransactionType.Reserve, 1, null,
            InventoryReferenceType.Order, _orderId, "same-key", _userId, null, null);
        var second = new InventoryMutationCommand(
            _lowId, InventoryTransactionType.Sale, 1, null,
            InventoryReferenceType.Order, _orderId, "same-key", _userId, null, null);

        await _service.ApplyBatchAsync(new[] { first }, CancellationToken.None);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ApplyBatchAsync(new[] { second }, CancellationToken.None));
    }

    [Fact]
    public async Task ApplyBatchAsync_WithMissingInventory_Throws()
    {
        await SeedInventoriesAsync();
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var missing = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ApplyBatchAsync(
                new[] { InventoryMutationCommand.Reserve(missing, 1, _orderId, _userId) },
                CancellationToken.None));
    }

    [Fact]
    public async Task ApplyBatchAsync_ReserveBeyondAvailable_ThrowsWithoutLedgerRows()
    {
        await SeedInventoriesAsync();
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ApplyBatchAsync(
                new[] { InventoryMutationCommand.Reserve(_lowId, 999, _orderId, _userId) },
                CancellationToken.None));

        await _db.SaveChangesAsync();
        Assert.Equal(0, await _db.InventoryTransactions.CountAsync());
    }

    [Fact]
    public async Task ApplyBatchAsync_ManualAdjustment_RecordsAbsoluteSnapshotAndActor()
    {
        await SeedInventoriesAsync();
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        await _service.ApplyBatchAsync(
            new[] { InventoryMutationCommand.ManualAdjustment(_lowId, 20, _userId, "restock") },
            CancellationToken.None);
        await _db.SaveChangesAsync();

        var inventory = await _db.BranchInventories.SingleAsync(bi => bi.Id == _lowId);
        Assert.Equal(20, inventory.QuantityOnHand);

        var ledger = await _db.InventoryTransactions.SingleAsync();
        Assert.Equal(InventoryTransactionType.ManualAdjustment, ledger.TransactionType);
        Assert.Equal(-80, ledger.QuantityOnHandDelta);
        Assert.Equal(0, ledger.ReservedQuantityDelta);
        Assert.Equal(20, ledger.QuantityOnHandAfter);
        Assert.Equal(_userId, ledger.ActorUserId);
        Assert.Equal("restock", ledger.Note);
    }
}