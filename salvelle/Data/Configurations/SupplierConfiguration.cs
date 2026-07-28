using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Pharmacy;

namespace Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        // CreatedAt - Default automático no banco
        builder.Property(s => s.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        // UpdatedAt - Default automático no banco
        builder.Property(s => s.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        // Status - Default
        builder.Property(s => s.Status)
            .HasDefaultValue("Em Avaliação");

        // Country - Default
        builder.Property(s => s.Country)
            .HasDefaultValue("Brasil");

        // IsActive - Default
        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        // IsQualified - Default
        builder.Property(s => s.IsQualified)
            .HasDefaultValue(false);

        // IsPreferred - Default
        builder.Property(s => s.IsPreferred)
            .HasDefaultValue(false);

        // HasGmpCertificate - Default
        builder.Property(s => s.HasGmpCertificate)
            .HasDefaultValue(false);

        // HasIsoCertificate - Default
        builder.Property(s => s.HasIsoCertificate)
            .HasDefaultValue(false);

        // HasAnvisaAuthorization - Default
        builder.Property(s => s.HasAnvisaAuthorization)
            .HasDefaultValue(false);

        // TotalOrders - Default
        builder.Property(s => s.TotalOrders)
            .HasDefaultValue(0);

        // NonConformitiesCount - Default
        builder.Property(s => s.NonConformitiesCount)
            .HasDefaultValue(0);

        // Relacionamentos
        builder.HasMany(s => s.Contacts)
            .WithOne(c => c.Supplier)
            .HasForeignKey(c => c.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Certificates)
            .WithOne(c => c.Supplier)
            .HasForeignKey(c => c.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Evaluations)
            .WithOne(e => e.Supplier)
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(s => s.Cnpj);
        builder.HasIndex(s => new { s.EstablishmentId, s.IsActive });
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.Classification);
    }
}
