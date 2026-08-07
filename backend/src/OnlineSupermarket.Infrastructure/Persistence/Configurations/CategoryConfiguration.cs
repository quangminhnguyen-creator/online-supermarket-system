using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSupermarket.Domain.Catalog;

namespace OnlineSupermarket.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).HasColumnName("id").HasColumnType("char(36)").ValueGeneratedNever();
        builder.Property(category => category.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(category => category.Slug).HasColumnName("slug").HasMaxLength(140).IsRequired();
        builder.Property(category => category.ParentCategoryId).HasColumnName("parent_category_id").HasColumnType("char(36)");
        builder.Property(category => category.IsActive).HasColumnName("is_active");
        builder.HasIndex(category => category.Slug).IsUnique();
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(category => category.ParentCategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
