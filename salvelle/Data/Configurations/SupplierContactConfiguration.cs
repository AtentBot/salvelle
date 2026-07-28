using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Pharmacy;

namespace Data.Configurations;

public class SupplierContactConfiguration : IEntityTypeConfiguration<SupplierContact>
{
    public void Configure(EntityTypeBuilder<SupplierContact> builder)
    {
        // CreatedAt - Default automático no banco
        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        // UpdatedAt - Default automático no banco
        builder.Property(c => c.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        // IsActive - Default
        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        // IsPrimary - Default
        builder.Property(c => c.IsPrimary)
            .HasDefaultValue(false);

        // IsEmergencyContact - Default
        builder.Property(c => c.IsEmergencyContact)
            .HasDefaultValue(false);

        // Índices
        builder.HasIndex(c => new { c.SupplierId, c.IsPrimary, c.IsActive });
    }
}
