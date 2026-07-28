using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Employees;

namespace Data.Configurations;

public class JobPositionConfiguration : IEntityTypeConfiguration<JobPosition>
{
    public void Configure(EntityTypeBuilder<JobPosition> builder)
    {
        builder.ToTable("job_positions");

        builder.HasIndex(e => e.Code)
            .IsUnique()
            .HasDatabaseName("ix_job_positions_code");

        builder.HasIndex(e => e.EstablishmentId)
            .HasDatabaseName("ix_job_positions_establishment_id");

        builder.HasOne(jp => jp.Establishment)
            .WithMany()
            .HasForeignKey(jp => jp.EstablishmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(jp => jp.SuperiorPosition)
            .WithMany()
            .HasForeignKey(jp => jp.ReportsTo)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(jp => jp.SuggestedSalaryMin)
            .HasPrecision(10, 2);

        builder.Property(jp => jp.SuggestedSalaryMax)
            .HasPrecision(10, 2);

        builder.Property(jp => jp.IsActive)
            .HasDefaultValue(true);

        builder.Property(jp => jp.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(jp => jp.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
