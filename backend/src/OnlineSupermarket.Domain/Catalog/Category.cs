using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Catalog;

public sealed class Category : Entity
{
    private Category()
    {
    }

    public Category(string name, string slug, Guid? parentCategoryId = null)
        : base(Guid.NewGuid())
    {
        Name = Guard.Required(name, nameof(name));
        Slug = Guard.Required(slug, nameof(slug));
        ParentCategoryId = parentCategoryId;
    }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Guid? ParentCategoryId { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(string name, string slug, Guid? parentCategoryId)
    {
        Name = Guard.Required(name, nameof(name));
        Slug = Guard.Required(slug, nameof(slug));
        ParentCategoryId = parentCategoryId;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
