using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Catalog;

public sealed class Product : Entity
{
    private Product()
    {
    }

    public Product(
        Guid categoryId,
        Guid brandId,
        string sku,
        string name,
        string slug,
        string? description,
        decimal basePrice,
        string unit,
        string? imageUrl)
        : base(Guid.NewGuid())
    {
        if (basePrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(basePrice));
        }

        CategoryId = categoryId;
        BrandId = brandId;
        Sku = Guard.Required(sku, nameof(sku));
        Name = Guard.Required(name, nameof(name));
        Slug = Guard.Required(slug, nameof(slug));
        Description = description?.Trim();
        BasePrice = basePrice;
        Unit = Guard.Required(unit, nameof(unit));
        ImageUrl = imageUrl?.Trim();
    }

    public Guid CategoryId { get; private set; }
    public Guid BrandId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal BasePrice { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
}
