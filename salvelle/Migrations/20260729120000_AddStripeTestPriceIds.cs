using Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations
{
    /// <summary>
    /// Adiciona as colunas de Stripe Price ID do ambiente de TESTE (Sandbox) na tabela
    /// subscription_plans. As colunas sem sufixo (stripe_price_id_monthly/_yearly) passam
    /// a representar os preços de PRODUÇÃO (Live), enquanto as novas *_test guardam os
    /// preços do modo de teste. Isso permite que o switch de ambiente no painel admin
    /// alterne também os preços, não só as chaves do gateway.
    /// Usa ADD COLUMN IF NOT EXISTS para ser idempotente (o banco de dev é o mesmo de prod).
    /// </summary>
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260729120000_AddStripeTestPriceIds")]
    public partial class AddStripeTestPriceIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE subscription_plans ADD COLUMN IF NOT EXISTS stripe_price_id_monthly_test character varying(255);
ALTER TABLE subscription_plans ADD COLUMN IF NOT EXISTS stripe_price_id_yearly_test character varying(255);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE subscription_plans DROP COLUMN IF EXISTS stripe_price_id_monthly_test;
ALTER TABLE subscription_plans DROP COLUMN IF EXISTS stripe_price_id_yearly_test;
");
        }
    }
}
