using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations
{
    /// <inheritdoc />
    public partial class FixLoginAttemptsTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Só corrige o nome da tabela para snake_case QUANDO existir a versão Pascal
            // ("LoginAttempts") e a snake ainda não. Em prod/dev a tabela já é
            // "login_attempts" (colunas idênticas), então isto é um NO-OP seguro —
            // apenas sincroniza o modelo/snapshot com a realidade do banco.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regclass('public."LoginAttempts"') IS NOT NULL
                       AND to_regclass('public.login_attempts') IS NULL THEN
                        ALTER TABLE "LoginAttempts" RENAME TO login_attempts;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regclass('public.login_attempts') IS NOT NULL
                       AND to_regclass('public."LoginAttempts"') IS NULL THEN
                        ALTER TABLE login_attempts RENAME TO "LoginAttempts";
                    END IF;
                END $$;
                """);
        }
    }
}
