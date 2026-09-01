using OnlineSupermarket.Domain.Common;

namespace OnlineSupermarket.Domain.Catalog;

public sealed class Brand : Entity
{
    private Brand()
    {
    }

    public Brand(string name, string slug)
        : base(Guid.NewGuid())
    {
        Name = Guard.Required(name, nameof(name));
        Slug = Guard.Required(slug, nameof(slug));
    }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public void Update(string name, string slug)
    {
        var validatedName = Guard.Required(name, nameof(name));
        var validatedSlug = Guard.Required(slug, nameof(slug));

        Name = validatedName;
        Slug = validatedSlug;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
