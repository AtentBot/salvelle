using Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations
{
    /// <summary>
    /// Portaria 344/98: adiciona flags de controle sanitário ao CatalogProduct
    /// (is_controlled, requires_prescription). Medicamento controlado ou sob prescrição
    /// NÃO pode ser vendido OTC pelo marketplace — o app passa a ocultá-lo das listagens
    /// (busca/farmácias) e a bloqueá-lo no carrinho e no checkout.
    /// Idempotente (ADD COLUMN IF NOT EXISTS) — o banco de dev/EnsureCreated pode já ter as colunas.
    /// </summary>
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260730190000_AddControlledFlagsToCatalogProduct")]
    public partial class AddControlledFlagsToCatalogProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""CatalogProducts"" ADD COLUMN IF NOT EXISTS is_controlled boolean NOT NULL DEFAULT false;
ALTER TABLE ""CatalogProducts"" ADD COLUMN IF NOT EXISTS requires_prescription boolean NOT NULL DEFAULT false;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""CatalogProducts"" DROP COLUMN IF EXISTS is_controlled;
ALTER TABLE ""CatalogProducts"" DROP COLUMN IF EXISTS requires_prescription;
");
        }
    }
}
