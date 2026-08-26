using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations
{
    /// <inheritdoc />
    public partial class MultiTenantIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_employees_cpf",
                table: "employees");

            migrationBuilder.AddColumn<Guid>(
                name: "IdentityId",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentEstablishmentId",
                table: "employee_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "identities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Cpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    WhatsApp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FullName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    PasswordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PasswordAlgorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequirePasswordChange = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employees_cpf",
                table: "employees",
                column: "Cpf");

            migrationBuilder.CreateIndex(
                name: "ix_employees_identity_establishment",
                table: "employees",
                columns: new[] { "IdentityId", "EstablishmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_sessions_CurrentEstablishmentId",
                table: "employee_sessions",
                column: "CurrentEstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_identities_Cpf",
                table: "identities",
                column: "Cpf",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_employee_sessions_Establishments_CurrentEstablishmentId",
                table: "employee_sessions",
                column: "CurrentEstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_employees_identities_IdentityId",
                table: "employees",
                column: "IdentityId",
                principalTable: "identities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ── Backfill multi-tenant ─────────────────────────────────────────
            // Cria 1 identidade por CPF a partir dos employees existentes (credencial
            // do employee mais antigo), vincula os employees e preenche a loja ativa
            // das sessões abertas. Idempotente: só age em linhas ainda não vinculadas.
            migrationBuilder.Sql("""
                INSERT INTO identities ("Id","Cpf","WhatsApp","Email","FullName","PasswordHash","PasswordCreatedAt","PasswordAlgorithm","RequirePasswordChange","TwoFactorEnabled","FailedLoginAttempts","LockedUntil","CreatedAt","UpdatedAt")
                SELECT gen_random_uuid(), e."Cpf", e."WhatsApp", e."Email", e."FullName",
                       e."PasswordHash", e."PasswordCreatedAt", e."PasswordAlgorithm", e."RequirePasswordChange",
                       e."TwoFactorEnabled", e."FailedLoginAttempts", e."LockedUntil", now(), now()
                FROM (
                    SELECT DISTINCT ON ("Cpf") *
                    FROM employees
                    WHERE "Cpf" IS NOT NULL AND "PasswordHash" IS NOT NULL
                    ORDER BY "Cpf", "CreatedAt" ASC
                ) e
                WHERE NOT EXISTS (SELECT 1 FROM identities i WHERE i."Cpf" = e."Cpf");

                UPDATE employees emp
                SET "IdentityId" = i."Id"
                FROM identities i
                WHERE emp."Cpf" = i."Cpf" AND emp."IdentityId" IS NULL;

                UPDATE employee_sessions s
                SET "CurrentEstablishmentId" = e."EstablishmentId"
                FROM employees e
                WHERE s."EmployeeId" = e."Id" AND s."CurrentEstablishmentId" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employee_sessions_Establishments_CurrentEstablishmentId",
                table: "employee_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_employees_identities_IdentityId",
                table: "employees");

            migrationBuilder.DropTable(
                name: "identities");

            migrationBuilder.DropIndex(
                name: "ix_employees_cpf",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_identity_establishment",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employee_sessions_CurrentEstablishmentId",
                table: "employee_sessions");

            migrationBuilder.DropColumn(
                name: "IdentityId",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "CurrentEstablishmentId",
                table: "employee_sessions");

            migrationBuilder.CreateIndex(
                name: "ix_employees_cpf",
                table: "employees",
                column: "Cpf",
                unique: true);
        }
    }
}
