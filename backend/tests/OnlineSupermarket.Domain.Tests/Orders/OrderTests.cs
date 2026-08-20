using OnlineSupermarket.Domain.Orders;

namespace OnlineSupermarket.Domain.Tests.Orders;

public sealed class OrderTests
{
    [Fact]
    public void Create_WithValidItems_CreatesOrderInPendingStatus()
    {
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var items = new List<(Guid, string, string, decimal, int, decimal)>
        {
            (Guid.NewGuid(), "Product A", "SKU001", 100m, 2, 200m)
        };

        var order = Order.Create(
            userId, branchId, "Pickup", "Test User", "0123456789",
            "Pickup at branch", null, items, 200m, 0m, 0m, 200m);

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Single(order.Items);
        Assert.Equal(200m, order.TotalAmount);
        Assert.Single(order.StatusHistory);
    }

    [Fact]
    public void SetStatus_AddsHistoryEntry()
    {
        var order = CreateTestOrder();

        order.SetStatus(OrderStatus.Confirmed, "Payment received");

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(2, order.StatusHistory.Count);
        Assert.Contains(order.StatusHistory, h => h.ToStatus == OrderStatus.Confirmed);
    }

    [Fact]
    public void Create_WithDelivery_IncludesDeliveryAddress()
    {
        var items = CreateTestItems();
        var order = Order.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Delivery",
            "John Doe", "0123456789",
            "123 Main St, District 1, HCMC",
            null, items, 100m, 0m, 15000m, 16000m);

        Assert.Equal("Delivery", order.FulfillmentType);
        Assert.Equal(15000m, order.ShippingFee);
        Assert.Equal(16000m, order.TotalAmount);
    }

    [Fact]
    public void Create_WithDiscount_CalculatesCorrectTotal()
    {
        var items = CreateTestItems();
        var order = Order.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Pickup", "Test", "0123",
            "Pickup", null, items, 100m, 10m, 0m, 90m);

        Assert.Equal(100m, order.Subtotal);
        Assert.Equal(10m, order.DiscountAmount);
        Assert.Equal(90m, order.TotalAmount);
    }

    private static Order CreateTestOrder()
    {
        var items = new List<(Guid, string, string, decimal, int, decimal)>
        {
            (Guid.NewGuid(), "Product A", "SKU001", 100m, 1, 100m)
        };
        return Order.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Pickup", "Test", "0123",
            "Pickup", null, items, 100m, 0m, 0m, 100m);
    }

    private static List<(Guid, string, string, decimal, int, decimal)> CreateTestItems()
    {
        return new List<(Guid, string, string, decimal, int, decimal)>
        {
            (Guid.NewGuid(), "Product A", "SKU001", 100m, 1, 100m)
        };
    }
}
