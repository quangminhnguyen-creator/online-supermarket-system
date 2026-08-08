using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSupermarket.Domain.Catalog;

namespace OnlineSupermarket.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).HasColumnName("id").HasColumnType("char(36)").ValueGeneratedNever();
        builder.Property(product => product.CategoryId).HasColumnName("category_id").HasColumnType("char(36)");
        builder.Property(product => product.BrandId).HasColumnName("brand_id").HasColumnType("char(36)");
        builder.Property(product => product.Sku).HasColumnName("sku").HasMaxLength(64).IsRequired();
        builder.Property(product => product.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(product => product.Slug).HasColumnName("slug").HasMaxLength(220).IsRequired();
        builder.Property(product => product.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(product => product.BasePrice).HasColumnName("base_price").HasPrecision(18, 2);
        builder.Property(product => product.Unit).HasColumnName("unit").HasMaxLength(30).IsRequired();
        builder.Property(product => product.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
        builder.Property(product => product.IsActive).HasColumnName("is_active");
        builder.HasIndex(product => product.Sku).IsUnique();
        builder.HasIndex(product => product.Slug).IsUnique();
        builder.HasOne<Category>().WithMany().HasForeignKey(product => product.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Brand>().WithMany().HasForeignKey(product => product.BrandId).OnDelete(DeleteBehavior.Restrict);
    }
}
