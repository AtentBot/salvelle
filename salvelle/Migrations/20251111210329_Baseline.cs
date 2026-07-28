using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Migrations
{
    /// <inheritdoc />
    public partial class Baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BASELINE (NO-OP): não criar nem alterar nada.
            // As tabelas abaixo já existem no banco e NÃO devem ser recriadas:
            // AccessLevels
            // AccessProfiles
            // Batches
            // Categories
            // Establishments
            // FormulaComponents
            // Formulas
            // ManipulationOrders
            // RawMaterials
            // SaleItems
            // Sales
            // Sessions
            // StockMovements
            // Suppliers
            // __EFMigrationsHistory
            // client_onboarding
            // employee_benefits
            // employee_documents
            // employee_job_history
            // employee_sessions
            // employees
            // job_positions
            // permissions
            // role_permissions
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}
