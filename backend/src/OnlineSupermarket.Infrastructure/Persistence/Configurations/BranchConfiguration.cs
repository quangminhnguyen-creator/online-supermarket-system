using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSupermarket.Domain.Branches;

namespace OnlineSupermarket.Infrastructure.Persistence.Configurations;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches");
        builder.HasKey(branch => branch.Id);
        builder.Property(branch => branch.Id).HasColumnName("id").HasColumnType("char(36)").ValueGeneratedNever();
        builder.Property(branch => branch.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(branch => branch.Address).HasColumnName("address").HasMaxLength(300).IsRequired();
        builder.Property(branch => branch.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(branch => branch.Latitude).HasColumnName("latitude").HasPrecision(10, 7);
        builder.Property(branch => branch.Longitude).HasColumnName("longitude").HasPrecision(10, 7);
        builder.Property(branch => branch.IsActive).HasColumnName("is_active");
    }
}
