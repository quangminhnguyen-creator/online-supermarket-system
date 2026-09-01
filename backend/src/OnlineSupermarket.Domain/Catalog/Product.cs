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

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category id is required.", nameof(categoryId));
        }

        if (brandId == Guid.Empty)
        {
            throw new ArgumentException("Brand id is required.", nameof(brandId));
        }

        var newSku = Guard.Required(sku, nameof(sku));
        var newName = Guard.Required(name, nameof(name));
        var newSlug = Guard.Required(slug, nameof(slug));
        var newUnit = Guard.Required(unit, nameof(unit));

        CategoryId = categoryId;
        BrandId = brandId;
        Sku = newSku;
        Name = newName;
        Slug = newSlug;
        Description = description?.Trim();
        BasePrice = basePrice;
        Unit = newUnit;
        ImageUrl = imageUrl?.Trim();
    }

    public Guid CategoryId { get; private set; }

    public void ChangeCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category id is required.", nameof(categoryId));
        }

        CategoryId = categoryId;
    }
    public Guid BrandId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal BasePrice { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(
        Guid categoryId,
        Guid brandId,
        string sku,
        string name,
        string slug,
        string? description,
        decimal basePrice,
        string unit,
        string? imageUrl)
    {
        if (basePrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(basePrice));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category id is required.", nameof(categoryId));
        }

        if (brandId == Guid.Empty)
        {
            throw new ArgumentException("Brand id is required.", nameof(brandId));
        }

        var newSku = Guard.Required(sku, nameof(sku));
        var newName = Guard.Required(name, nameof(name));
        var newSlug = Guard.Required(slug, nameof(slug));
        var newUnit = Guard.Required(unit, nameof(unit));

        CategoryId = categoryId;
        BrandId = brandId;
        Sku = newSku;
        Name = newName;
        Slug = newSlug;
        Description = description?.Trim();
        BasePrice = basePrice;
        Unit = newUnit;
        ImageUrl = imageUrl?.Trim();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    // Navigation properties (EF Core managed)
    public Category? Category { get; private set; }
    public Brand? Brand { get; private set; }
}
