using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations
{
    /// <inheritdoc />
    public partial class AddCustomersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Batches_ApprovedByEmployeeId",
                table: "Batches",
                column: "ApprovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_CreatedByEmployeeId",
                table: "Batches",
                column: "CreatedByEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_employees_ApprovedByEmployeeId",
                table: "Batches",
                column: "ApprovedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_employees_CreatedByEmployeeId",
                table: "Batches",
                column: "CreatedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Batches_employees_ApprovedByEmployeeId",
                table: "Batches");

            migrationBuilder.DropForeignKey(
                name: "FK_Batches_employees_CreatedByEmployeeId",
                table: "Batches");

            migrationBuilder.DropIndex(
                name: "IX_Batches_ApprovedByEmployeeId",
                table: "Batches");

            migrationBuilder.DropIndex(
                name: "IX_Batches_CreatedByEmployeeId",
                table: "Batches");
        }
    }
}
