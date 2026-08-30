using OnlineSupermarket.Domain.Branches;
using OnlineSupermarket.Domain.Catalog;

namespace OnlineSupermarket.Domain.Tests.Catalog;

public sealed class CatalogEntityTests
{
    [Fact]
    public void Branch_WithBlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Branch(" ", "01 Nguyễn Huệ", "0900000000", 10.77m, 106.70m));
    }

    [Fact]
    public void Category_WithBlankSlug_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Category("Rau củ", " "));
    }

    [Fact]
    public void Brand_WithBlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Brand(" ", "vinamilk"));
    }

    [Fact]
    public void Product_WithNegativeBasePrice_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Product(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-001", "Sữa tươi",
            "sua-tuoi", "Hộp 1L", -1m, "hộp", null));
    }

    [Fact]
    public void Product_WithEmptyCategoryId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Product(
            Guid.Empty, Guid.NewGuid(), "SKU-001", "Sản phẩm",
            "san-pham", null, 100_000m, "cái", null));
    }

    [Fact]
    public void ChangeCategory_WithLeafCategory_UpdatesCategoryId()
    {
        var product = new Product(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-001", "Sản phẩm",
            "san-pham", null, 100_000m, "cái", null);
        var leafCategoryId = Guid.NewGuid();

        product.ChangeCategory(leafCategoryId);

        Assert.Equal(leafCategoryId, product.CategoryId);
    }

    [Fact]
    public void ChangeCategory_WithEmptyCategoryId_Throws()
    {
        var product = new Product(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-001", "Sản phẩm",
            "san-pham", null, 100_000m, "cái", null);

        Assert.Throws<ArgumentException>(() => product.ChangeCategory(Guid.Empty));
    }
}
