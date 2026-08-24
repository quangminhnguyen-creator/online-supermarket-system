using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSupermarket.Domain.Entities;

namespace OnlineSupermarket.Infrastructure.Persistence.Configurations;

internal sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");
        builder.HasKey(address => address.Id);
        builder.Property(address => address.Id).HasColumnName("id").HasColumnType("char(36)").ValueGeneratedNever();
        builder.Property(address => address.UserId).HasColumnName("user_id").HasColumnType("char(36)").IsRequired();
        builder.Property(address => address.RecipientName).HasColumnName("recipient_name").HasMaxLength(150).IsRequired();
        builder.Property(address => address.Phone).HasColumnName("phone").HasMaxLength(20).IsRequired();
        builder.Property(address => address.Street).HasColumnName("street").HasMaxLength(500).IsRequired();
        builder.Property(address => address.Ward).HasColumnName("ward").HasMaxLength(100).IsRequired();
        builder.Property(address => address.District).HasColumnName("district").HasMaxLength(100).IsRequired();
        builder.Property(address => address.City).HasColumnName("city").HasMaxLength(100).IsRequired();
        builder.Property(address => address.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
        builder.Property(address => address.IsDefault).HasColumnName("is_default").HasDefaultValue(false).IsRequired();
        builder.Property(address => address.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("datetime(6)").IsRequired();
        builder.Property(address => address.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("datetime(6)").IsRequired();

        builder.HasIndex(address => address.UserId);
        builder.HasIndex(address => new { address.UserId, address.IsDefault });
    }
}
