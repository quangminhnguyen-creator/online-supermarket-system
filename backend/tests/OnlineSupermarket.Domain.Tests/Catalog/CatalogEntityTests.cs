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
    public void Product_WithEmptyBrandId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Product(
            Guid.NewGuid(), Guid.Empty, "SKU-001", "Sản phẩm",
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

    private static Product CreateProduct()
    {
        return new Product(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Sản phẩm 1",
            "san-pham-1", "Mô tả 1", 100_000m, "cái", "http://img/1.jpg");
    }

    [Fact]
    public void Category_UpdateAndDeactivate_ChangesMutableState()
    {
        var category = new Category("Cũ", "cu");
        var parentId = Guid.NewGuid();
        category.Update(" Mới ", " moi ", parentId);
        category.Deactivate();
        Assert.Equal("Mới", category.Name);
        Assert.Equal("moi", category.Slug);
        Assert.Equal(parentId, category.ParentCategoryId);
        Assert.False(category.IsActive);
        category.Activate();
        Assert.True(category.IsActive);
    }

    [Fact]
    public void Brand_UpdateAndDeactivate_ChangesMutableState()
    {
        var brand = new Brand("Cũ", "cu");
        brand.Update(" Mới ", " moi ");
        brand.Deactivate();
        Assert.Equal("Mới", brand.Name);
        Assert.Equal("moi", brand.Slug);
        Assert.False(brand.IsActive);
        brand.Activate();
        Assert.True(brand.IsActive);
    }

    [Fact]
    public void Product_UpdateAndDeactivate_ChangesMutableState()
    {
        var product = CreateProduct();
        var newCategoryId = Guid.NewGuid();
        var newBrandId = Guid.NewGuid();

        product.Update(newCategoryId, newBrandId, " SKU-2 ", " Tên Mới ", " ten-moi ", " Mô tả mới ", 200_000m, " hộp ", " http://img/2.jpg ");
        product.Deactivate();

        Assert.Equal(newCategoryId, product.CategoryId);
        Assert.Equal(newBrandId, product.BrandId);
        Assert.Equal("SKU-2", product.Sku);
        Assert.Equal("Tên Mới", product.Name);
        Assert.Equal("ten-moi", product.Slug);
        Assert.Equal("Mô tả mới", product.Description);
        Assert.Equal(200_000m, product.BasePrice);
        Assert.Equal("hộp", product.Unit);
        Assert.Equal("http://img/2.jpg", product.ImageUrl);
        Assert.False(product.IsActive);

        product.Activate();
        Assert.True(product.IsActive);
    }

    [Fact]
    public void Product_Update_WithNegativePrice_Throws()
    {
        var product = CreateProduct();
        Assert.Throws<ArgumentOutOfRangeException>(() => product.Update(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-2", "Tên", "ten", null, -1m, "cái", null));
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "d41a7741-9457-4b53-8408-0138947f68c3", "SKU-2", "Tên", "ten", 100000, "cái")] // Empty CategoryId
    [InlineData("d41a7741-9457-4b53-8408-0138947f68c3", "00000000-0000-0000-0000-000000000000", "SKU-2", "Tên", "ten", 100000, "cái")] // Empty BrandId
    [InlineData("d41a7741-9457-4b53-8408-0138947f68c3", "d41a7741-9457-4b53-8408-0138947f68c4", " ", "Tên", "ten", 100000, "cái")] // Blank Sku
    [InlineData("d41a7741-9457-4b53-8408-0138947f68c3", "d41a7741-9457-4b53-8408-0138947f68c4", "SKU-2", " ", "ten", 100000, "cái")] // Blank Name
    [InlineData("d41a7741-9457-4b53-8408-0138947f68c3", "d41a7741-9457-4b53-8408-0138947f68c4", "SKU-2", "Tên", " ", 100000, "cái")] // Blank Slug
    [InlineData("d41a7741-9457-4b53-8408-0138947f68c3", "d41a7741-9457-4b53-8408-0138947f68c4", "SKU-2", "Tên", "ten", -1, "cái")] // Negative BasePrice
    [InlineData("d41a7741-9457-4b53-8408-0138947f68c3", "d41a7741-9457-4b53-8408-0138947f68c4", "SKU-2", "Tên", "ten", 100000, " ")] // Blank Unit
    public void Product_Update_WithInvalidInputs_ThrowsAndKeepsOriginalState(
        string categoryIdStr, string brandIdStr, string sku, string name, string slug, decimal basePrice, string unit)
    {
        var product = CreateProduct();
        var originalCategoryId = product.CategoryId;
        var originalBrandId = product.BrandId;
        var originalSku = product.Sku;
        var originalName = product.Name;
        var originalSlug = product.Slug;
        var originalDescription = product.Description;
        var originalBasePrice = product.BasePrice;
        var originalUnit = product.Unit;
        var originalImageUrl = product.ImageUrl;

        var categoryId = Guid.Parse(categoryIdStr);
        var brandId = Guid.Parse(brandIdStr);

        Assert.ThrowsAny<ArgumentException>(() => product.Update(
            categoryId, brandId, sku, name, slug, "Mô tả mới", basePrice, unit, "http://img/2.jpg"));

        Assert.Equal(originalCategoryId, product.CategoryId);
        Assert.Equal(originalBrandId, product.BrandId);
        Assert.Equal(originalSku, product.Sku);
        Assert.Equal(originalName, product.Name);
        Assert.Equal(originalSlug, product.Slug);
        Assert.Equal(originalDescription, product.Description);
        Assert.Equal(originalBasePrice, product.BasePrice);
        Assert.Equal(originalUnit, product.Unit);
        Assert.Equal(originalImageUrl, product.ImageUrl);
    }

    [Fact]
    public void Category_Update_WithSelfParent_ThrowsAndKeepsOriginalState()
    {
        var category = new Category("Điện tử", "dien-tu");
        Assert.Throws<ArgumentException>(() => category.Update("Gia dụng", "gia-dung", category.Id));
        Assert.Equal("Điện tử", category.Name);
        Assert.Equal("dien-tu", category.Slug);
        Assert.Null(category.ParentCategoryId);
    }

    [Theory]
    [InlineData("", "slug-moi")]
    [InlineData("   ", "slug-moi")]
    [InlineData("Tên Mới", "")]
    [InlineData("Tên Mới", "   ")]
    public void Category_Update_WithInvalidInputs_ThrowsAndKeepsOriginalState(string name, string slug)
    {
        var category = new Category("Ban đầu", "ban-dau");
        Assert.Throws<ArgumentException>(() => category.Update(name, slug, null));
        Assert.Equal("Ban đầu", category.Name);
        Assert.Equal("ban-dau", category.Slug);
    }

    [Theory]
    [InlineData("", "slug-moi")]
    [InlineData("   ", "slug-moi")]
    [InlineData("Tên Mới", "")]
    [InlineData("Tên Mới", "   ")]
    public void Brand_Update_WithInvalidInputs_ThrowsAndKeepsOriginalState(string name, string slug)
    {
        var brand = new Brand("Ban đầu", "ban-dau");
        Assert.Throws<ArgumentException>(() => brand.Update(name, slug));
        Assert.Equal("Ban đầu", brand.Name);
        Assert.Equal("ban-dau", brand.Slug);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "d41a7741-9457-4b53-8408-0138947f68c3", "SKU-1", "Name", "slug", 100, "cái")]
    [InlineData("d41a7741-9457-4b53-8408-0138947f68c3", "00000000-0000-0000-0000-000000000000", "SKU-1", "Name", "slug", 100, "cái")]
    [InlineData("d41a7741-9457-4b53-8408-0138947f68c3", "d41a7741-9457-4b53-8408-0138947f68c4", " ", "Name", "slug", 100, "cái")]
    [InlineData("d41a7741-9457-4b53-8408-0138947f68c3", "d41a7741-9457-4b53-8408-0138947f68c4", "SKU-1", " ", "slug", 100, "cái")]
    [InlineData("d41a7741-9457-4b53-8408-0138947f68c3", "d41a7741-9457-4b53-8408-0138947f68c4", "SKU-1", "Name", " ", 100, "cái")]
    [InlineData("d41a7741-9457-4b53-8408-0138947f68c3", "d41a7741-9457-4b53-8408-0138947f68c4", "SKU-1", "Name", "slug", -1, "cái")]
    [InlineData("d41a7741-9457-4b53-8408-0138947f68c3", "d41a7741-9457-4b53-8408-0138947f68c4", "SKU-1", "Name", "slug", 100, " ")]
    public void Product_Constructor_WithInvalidInputs_Throws(
        string categoryIdStr, string brandIdStr, string sku, string name, string slug, decimal basePrice, string unit)
    {
        var categoryId = Guid.Parse(categoryIdStr);
        var brandId = Guid.Parse(brandIdStr);
        Assert.ThrowsAny<ArgumentException>(() => new Product(
            categoryId, brandId, sku, name, slug, "Mô tả", basePrice, unit, null));
    }
}
