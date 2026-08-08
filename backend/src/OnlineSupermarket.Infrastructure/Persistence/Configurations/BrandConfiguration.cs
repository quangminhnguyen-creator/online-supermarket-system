using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSupermarket.Domain.Catalog;

namespace OnlineSupermarket.Infrastructure.Persistence.Configurations;

internal sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands");
        builder.HasKey(brand => brand.Id);
        builder.Property(brand => brand.Id).HasColumnName("id").HasColumnType("char(36)").ValueGeneratedNever();
        builder.Property(brand => brand.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(brand => brand.Slug).HasColumnName("slug").HasMaxLength(140).IsRequired();
        builder.Property(brand => brand.IsActive).HasColumnName("is_active");
        builder.HasIndex(brand => brand.Slug).IsUnique();
    }
}
