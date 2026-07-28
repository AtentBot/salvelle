using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations
{
    /// <summary>
    /// Migração de BASELINE (no-op). Seu único propósito é ressincronizar o
    /// AppDbContextModelSnapshot com o modelo atual, que já divergira do snapshot
    /// (tabelas support_tickets / support_ticket_messages / whatsapp_instance_status /
    /// pending_two_factor_sessions / revoked_jwts e ajustes de colunas/nulabilidade).
    ///
    /// Todas essas mudanças JÁ existem no banco de produção (aplicadas via DDL
    /// idempotente e EnsureCreated). Por isso Up()/Down() são intencionalmente
    /// vazios — recriar/alterar objetos já existentes falharia. Esta migração é
    /// registrada como aplicada no __EFMigrationsHistory de produção para que o
    /// MigrateAsync a ignore. NÃO adicione operações aqui; o schema-alvo vive no
    /// AppDbContextModelSnapshot.
    /// </summary>
    public partial class SyncModelSnapshot20260726 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Baseline no-op — ver resumo da classe.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Baseline no-op — ver resumo da classe.
        }
    }
}
