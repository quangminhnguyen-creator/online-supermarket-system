using OnlineSupermarket.Domain.Shopping;

namespace OnlineSupermarket.Domain.Tests.Shopping;

public sealed class CartTests
{
    [Fact]
    public void Create_WithValidInputs_CreatesCart()
    {
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var cart = new Cart(userId, branchId);

        Assert.Equal(userId, cart.UserId);
        Assert.Equal(branchId, cart.BranchId);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void AddItem_NewProduct_AddsToCart()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var productId = Guid.NewGuid();
        var inventoryId = Guid.NewGuid();

        var item = cart.AddItem(productId, inventoryId, 100m, 2);

        Assert.Single(cart.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(200m, item.LineTotal);
    }

    [Fact]
    public void AddItem_ExistingProduct_IncrementsQuantity()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var productId = Guid.NewGuid();
        cart.AddItem(productId, Guid.NewGuid(), 100m, 2);

        cart.AddItem(productId, Guid.NewGuid(), 100m, 3);

        Assert.Single(cart.Items);
        Assert.Equal(5, cart.Items[0].Quantity);
    }

    [Fact]
    public void UpdateItemQuantity_ValidQuantity_Updates()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var item = cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 100m, 2);

        cart.UpdateItemQuantity(item.Id, 5);

        Assert.Equal(5, cart.Items[0].Quantity);
    }

    [Fact]
    public void UpdateItemQuantity_ZeroOrLess_RemovesItem()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var item = cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 100m, 2);

        cart.UpdateItemQuantity(item.Id, 0);

        Assert.Empty(cart.Items);
    }

    [Fact]
    public void RemoveItem_ExistingItem_Removes()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        var item = cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 100m, 1);

        cart.RemoveItem(item.Id);

        Assert.Empty(cart.Items);
    }

    [Fact]
    public void ChangeBranch_ClearsItems()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 100m, 1);
        var newBranchId = Guid.NewGuid();

        cart.ChangeBranch(newBranchId);

        Assert.Empty(cart.Items);
        Assert.Equal(newBranchId, cart.BranchId);
    }

    [Fact]
    public void TotalItems_SumsAllQuantities()
    {
        var cart = new Cart(Guid.NewGuid(), Guid.NewGuid());
        cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 100m, 2);
        cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), 50m, 3);

        Assert.Equal(5, cart.TotalItems);
    }
}
