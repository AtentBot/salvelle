using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations
{
    /// <inheritdoc />
    public partial class AddRawMaterialsCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RawMaterials_DcbCode",
                table: "RawMaterials");

            migrationBuilder.CreateTable(
                name: "integration_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    config_json = table.Column<string>(type: "text", nullable: true),
                    last_test_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_test_success = table.Column<bool>(type: "boolean", nullable: true),
                    last_test_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "RawMaterialsCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DcbCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CasNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ControlType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AllowedUsage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PhysicalState = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DefaultPurityFactor = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    DefaultCorrectionFactor = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    Synonyms = table.Column<string>(type: "text", nullable: true),
                    Indications = table.Column<string>(type: "text", nullable: true),
                    Popularity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawMaterialsCatalog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_EstablishmentId_DcbCode",
                table: "RawMaterials",
                columns: new[] { "EstablishmentId", "DcbCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterialsCatalog_AllowedUsage",
                table: "RawMaterialsCatalog",
                column: "AllowedUsage");

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterialsCatalog_Category",
                table: "RawMaterialsCatalog",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterialsCatalog_DcbCode",
                table: "RawMaterialsCatalog",
                column: "DcbCode");

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterialsCatalog_IsActive",
                table: "RawMaterialsCatalog",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterialsCatalog_Name",
                table: "RawMaterialsCatalog",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterialsCatalog_Popularity",
                table: "RawMaterialsCatalog",
                column: "Popularity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "integration_configs");

            migrationBuilder.DropTable(
                name: "RawMaterialsCatalog");

            migrationBuilder.DropIndex(
                name: "IX_RawMaterials_EstablishmentId_DcbCode",
                table: "RawMaterials");

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_DcbCode",
                table: "RawMaterials",
                column: "DcbCode",
                unique: true);
        }
    }
}
