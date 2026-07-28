using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Pharmacy;

namespace Data.Configurations;

public class SupplierCertificateConfiguration : IEntityTypeConfiguration<SupplierCertificate>
{
    public void Configure(EntityTypeBuilder<SupplierCertificate> builder)
    {
        // CreatedAt - Default automático no banco
        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        // UpdatedAt - Default automático no banco
        builder.Property(c => c.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        // Status - Default
        builder.Property(c => c.Status)
            .HasDefaultValue("Válido");

        // IsActive - Default
        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        // AlertBeforeExpiry - Default
        builder.Property(c => c.AlertBeforeExpiry)
            .HasDefaultValue(true);

        // AlertDaysBefore - Default
        builder.Property(c => c.AlertDaysBefore)
            .HasDefaultValue(30);

        // Índices
        builder.HasIndex(c => new { c.SupplierId, c.IsActive });
        builder.HasIndex(c => c.ExpiryDate);
        builder.HasIndex(c => c.CertificateType);
    }
}