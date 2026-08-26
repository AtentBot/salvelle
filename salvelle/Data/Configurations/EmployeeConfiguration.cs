using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Employees;

namespace Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        // Índices
        // Multi-tenant: Cpf NÃO é mais único global (a mesma pessoa pode atuar em várias lojas).
        // A unicidade de login vive em UserIdentity.Cpf; aqui só indexamos p/ busca.
        builder.HasIndex(e => e.Cpf)
            .HasDatabaseName("ix_employees_cpf");

        // Um vínculo por (identidade, estabelecimento).
        builder.HasIndex(e => new { e.IdentityId, e.EstablishmentId })
            .IsUnique()
            .HasDatabaseName("ix_employees_identity_establishment");

        builder.HasIndex(e => e.EstablishmentId)
            .HasDatabaseName("ix_employees_establishment_id");

        builder.HasIndex(e => e.JobPositionId)
            .HasDatabaseName("ix_employees_job_position_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_employees_status");

        builder.HasIndex(e => e.Email)
            .HasDatabaseName("ix_employees_email");

        // Relacionamentos
        builder.HasOne(e => e.Establishment)
            .WithMany()
            .HasForeignKey(e => e.EstablishmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.JobPosition)
            .WithMany(jp => jp.Employees)
            .HasForeignKey(e => e.JobPositionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Propriedades computadas
        builder.Ignore(e => e.JobHistory);
        builder.Ignore(e => e.Sessions);
        builder.Ignore(e => e.Benefits);
        builder.Ignore(e => e.Documents);

        // Valores padrão
        builder.Property(e => e.Status)
            .HasDefaultValue("Ativo");

        builder.Property(e => e.ContractType)
            .HasDefaultValue("CLT");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Precisão decimal
        builder.Property(e => e.Salary)
            .HasPrecision(10, 2);
    }
}
