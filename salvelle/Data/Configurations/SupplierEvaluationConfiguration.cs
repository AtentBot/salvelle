using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Pharmacy;

namespace Data.Configurations;

public class SupplierEvaluationConfiguration : IEntityTypeConfiguration<SupplierEvaluation>
{
    public void Configure(EntityTypeBuilder<SupplierEvaluation> builder)
    {
        // EvaluationDate - Default automático no banco
        builder.Property(e => e.EvaluationDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        // CreatedAt - Default automático no banco
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        // UpdatedAt - Default automático no banco
        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        // IsApproved - Default
        builder.Property(e => e.IsApproved)
            .HasDefaultValue(true);

        // TotalOrders - Default
        builder.Property(e => e.TotalOrders)
            .HasDefaultValue(0);

        // OnTimeDeliveries - Default
        builder.Property(e => e.OnTimeDeliveries)
            .HasDefaultValue(0);

        // LateDeliveries - Default
        builder.Property(e => e.LateDeliveries)
            .HasDefaultValue(0);

        // NonConformities - Default
        builder.Property(e => e.NonConformities)
            .HasDefaultValue(0);

        // Returns - Default
        builder.Property(e => e.Returns)
            .HasDefaultValue(0);

        // Índices
        builder.HasIndex(e => new { e.SupplierId, e.EvaluationDate });
        builder.HasIndex(e => e.Classification);
        builder.HasIndex(e => e.IsApproved);
    }
}