namespace OnlineSupermarket.Infrastructure.Persistence.SeedData;

internal sealed record CategorySeedDefinition(
    string Name,
    string Slug,
    string? ParentSlug = null);

internal static class CatalogSeedTaxonomy
{
    internal const string UncategorizedSlug = "uncategorized";

    internal static IReadOnlyList<CategorySeedDefinition> Categories { get; } =
    [
        new("Điện thoại & Tablet", "dien-thoai-tablet"),
        new("Laptop & Máy tính", "laptop-may-tinh"),
        new("TV & Màn hình", "tv-man-hinh"),
        new("Thiết bị gia dụng", "thiet-bi-gia-dung"),
        new("Âm thanh & Loa", "am-thanh-loa"),
        new("Phụ kiện", "phu-kien"),
        new("Game & Gaming", "game-gaming"),
        new("Camera & An ninh", "camera-an-ninh"),
        new("Chưa phân loại", UncategorizedSlug),
        new("Điện thoại", "dien-thoai", "dien-thoai-tablet"),
        new("Máy tính bảng", "may-tinh-bang", "dien-thoai-tablet"),
        new("Laptop", "laptop", "laptop-may-tinh"),
        new("TV", "tivi", "tv-man-hinh"),
        new("Màn hình máy tính", "man-hinh-may-tinh", "tv-man-hinh"),
        new("Máy lạnh", "may-lanh", "thiet-bi-gia-dung"),
        new("Tủ lạnh", "tu-lanh", "thiet-bi-gia-dung"),
        new("Máy giặt", "may-giat", "thiet-bi-gia-dung"),
        new("Tai nghe", "tai-nghe", "am-thanh-loa"),
        new("Loa", "loa", "am-thanh-loa"),
        new("Chuột", "chuot", "phu-kien"),
        new("Máy chơi game", "may-choi-game", "game-gaming"),
        new("Máy ảnh", "may-anh", "camera-an-ninh"),
        new("Camera an ninh", "camera-giam-sat", "camera-an-ninh"),
    ];

    internal static IReadOnlyDictionary<string, string> ProductCategorySlugBySku { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DT-SAM-001"] = "dien-thoai",
            ["DT-SAM-002"] = "dien-thoai",
            ["DT-APP-001"] = "dien-thoai",
            ["DT-APP-002"] = "dien-thoai",
            ["DT-XIA-001"] = "dien-thoai",
            ["DT-SAM-003"] = "may-tinh-bang",
            ["DT-APP-003"] = "may-tinh-bang",
            ["LT-DEL-001"] = "laptop",
            ["LT-APP-001"] = "laptop",
            ["LT-APP-002"] = "laptop",
            ["LT-ASUS-001"] = "laptop",
            ["LT-ASUS-002"] = "laptop",
            ["TV-SAM-001"] = "tivi",
            ["TV-LG-001"] = "tivi",
            ["TV-SON-001"] = "tivi",
            ["MH-SAM-001"] = "man-hinh-may-tinh",
            ["GD-PAN-001"] = "may-lanh",
            ["GD-LG-001"] = "may-lanh",
            ["GD-LG-002"] = "tu-lanh",
            ["GD-PAN-002"] = "may-giat",
            ["AT-JBL-001"] = "loa",
            ["AT-SON-001"] = "tai-nghe",
            ["AT-JBL-002"] = "loa",
            ["PK-APP-001"] = "tai-nghe",
            ["PK-APP-002"] = "chuot",
            ["GM-SON-001"] = "may-choi-game",
            ["CAM-CAN-001"] = "may-anh",
            ["CAM-XIA-001"] = "camera-giam-sat",
        };

    internal static string ResolveProductCategorySlug(string? sku) =>
        !string.IsNullOrWhiteSpace(sku) &&
        ProductCategorySlugBySku.TryGetValue(sku, out var slug)
            ? slug
            : UncategorizedSlug;

    internal static Guid ResolveProductCategoryId(
        string? sku,
        IReadOnlyDictionary<string, Guid> categoryIdsBySlug)
    {
        var slug = ResolveProductCategorySlug(sku);
        return categoryIdsBySlug.TryGetValue(slug, out var categoryId)
            ? categoryId
            : throw new InvalidOperationException($"Required category '{slug}' was not seeded.");
    }
}