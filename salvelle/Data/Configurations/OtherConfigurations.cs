using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Employees;
using Models.Security;

namespace Data.Configurations;

public class EmployeeJobHistoryConfiguration : IEntityTypeConfiguration<EmployeeJobHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeJobHistory> builder)
    {
        builder.ToTable("employee_job_history");

        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => e.JobPositionId);
        builder.HasIndex(e => e.StartDate);

        builder.HasOne(ejh => ejh.Employee)
            .WithMany(e => e.JobHistory)
            .HasForeignKey(ejh => ejh.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ejh => ejh.JobPosition)
            .WithMany(jp => jp.JobHistories)
            .HasForeignKey(ejh => ejh.JobPositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ejh => ejh.SalaryAtTime).HasPrecision(10, 2);
        builder.Property(ejh => ejh.PreviousSalary).HasPrecision(10, 2);
    }
}

public class EmployeeSessionConfiguration : IEntityTypeConfiguration<EmployeeSession>
{
    public void Configure(EntityTypeBuilder<EmployeeSession> builder)
    {
        builder.ToTable("employee_sessions");

        builder.HasIndex(e => e.Token).IsUnique();
        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => e.ExpiresAt);
        builder.HasIndex(e => e.IsActive);

        builder.HasOne(es => es.Employee)
            .WithMany(e => e.Sessions)
            .HasForeignKey(es => es.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(es => es.IsActive).HasDefaultValue(true);
    }
}

public class EmployeeBenefitConfiguration : IEntityTypeConfiguration<EmployeeBenefit>
{
    public void Configure(EntityTypeBuilder<EmployeeBenefit> builder)
    {
        builder.ToTable("employee_benefits");

        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => e.BenefitType);
        builder.HasIndex(e => e.IsActive);

        builder.HasOne(eb => eb.Employee)
            .WithMany(e => e.Benefits)
            .HasForeignKey(eb => eb.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(eb => eb.MonthlyValue).HasPrecision(10, 2);
        builder.Property(eb => eb.EmployeeContribution).HasPrecision(10, 2);
        builder.Property(eb => eb.EmployerContribution).HasPrecision(10, 2);
        builder.Property(eb => eb.IsActive).HasDefaultValue(true);
    }
}

public class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.ToTable("employee_documents");

        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => e.DocumentType);
        builder.HasIndex(e => e.ExpiryDate);

        builder.HasOne(ed => ed.Employee)
            .WithMany(e => e.Documents)
            .HasForeignKey(ed => ed.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(ed => ed.Status).HasDefaultValue("Pendente");
        builder.Property(ed => ed.Version).HasDefaultValue(1);
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasIndex(p => p.ResourceAction).IsUnique();
        builder.HasIndex(p => p.Resource);
        builder.HasIndex(p => p.Category);

        builder.Property(p => p.Scope).HasDefaultValue("Own");
        builder.Property(p => p.RiskLevel).HasDefaultValue("Low");
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.Property(p => p.AuditLog).HasDefaultValue(true);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasIndex(rp => new { rp.JobPositionId, rp.PermissionId }).IsUnique();
        builder.HasIndex(rp => rp.EstablishmentId);

        builder.HasOne(rp => rp.JobPosition)
            .WithMany(jp => jp.RolePermissions)
            .HasForeignKey(rp => rp.JobPositionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Establishment)
            .WithMany()
            .HasForeignKey(rp => rp.EstablishmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(rp => rp.IsGranted).HasDefaultValue(true);
        builder.Property(rp => rp.IsPermanent).HasDefaultValue(true);
        builder.Property(rp => rp.IsActive).HasDefaultValue(true);
    }
}
