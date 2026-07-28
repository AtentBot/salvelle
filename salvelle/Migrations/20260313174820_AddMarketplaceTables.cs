using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Batches_RawMaterials_RawMaterialId",
                table: "Batches");

            migrationBuilder.DropForeignKey(
                name: "FK_Batches_suppliers_SupplierId",
                table: "Batches");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchReceivings_Batches_BatchId",
                table: "BatchReceivings");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchReceivings_PurchaseOrderItems_PurchaseOrderItemId",
                table: "BatchReceivings");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchReceivings_employees_ReceivedByEmployeeId",
                table: "BatchReceivings");

            migrationBuilder.DropForeignKey(
                name: "FK_Establishments_AccessLevels_AccessLevelId",
                table: "Establishments");

            migrationBuilder.DropForeignKey(
                name: "FK_FormulaComponents_RawMaterials_RawMaterialId",
                table: "FormulaComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_Formulas_Establishments_EstablishmentId",
                table: "Formulas");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_Establishments_EstablishmentId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_Formulas_FormulaId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_employees_ApprovedByPharmacistId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_employees_CheckedByEmployeeId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_employees_ManipulatedByEmployeeId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_employees_RequestedByEmployeeId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PasswordResetTokens_employees_EmployeeId",
                table: "PasswordResetTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_RawMaterials_RawMaterialId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Establishments_EstablishmentId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_employees_ApprovedByEmployeeId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_employees_CreatedByEmployeeId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_employees_UpdatedByEmployeeId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_suppliers_SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_RawMaterials_Establishments_EstablishmentId",
                table: "RawMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Establishments_EstablishmentId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_employees_AuthorizedByPharmacistId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_employees_SoldByEmployeeId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Batches_BatchId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Establishments_EstablishmentId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_RawMaterials_RawMaterialId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_suppliers_Establishments_EstablishmentId",
                table: "suppliers");

            migrationBuilder.DropTable(
                name: "SaleItems");

            migrationBuilder.DropIndex(
                name: "IX_TwoFactorAuths_Code_Purpose_ExpiresAt",
                table: "TwoFactorAuths");

            migrationBuilder.DropIndex(
                name: "IX_TwoFactorAuths_EmployeeId_ExpiresAt",
                table: "TwoFactorAuths");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sales",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_EstablishmentId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_SaleDate",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_SaleNumber_EstablishmentId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_Status",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_EstablishmentId_OrderDate",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_EstablishmentId_Status",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_LoginAttempts_AttemptedAt",
                table: "LoginAttempts");

            migrationBuilder.DropIndex(
                name: "IX_LoginAttempts_Identifier",
                table: "LoginAttempts");

            migrationBuilder.DropIndex(
                name: "IX_LoginAttempts_IpAddress_AttemptedAt",
                table: "LoginAttempts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PasswordResetTokens",
                table: "PasswordResetTokens");

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_Code_ExpiresAt",
                table: "PasswordResetTokens");

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_EmployeeId_ExpiresAt",
                table: "PasswordResetTokens");

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_Token",
                table: "PasswordResetTokens");

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"));

            migrationBuilder.DropColumn(
                name: "CustomerCpf",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PrescriberName",
                table: "Sales");

            migrationBuilder.RenameTable(
                name: "Sales",
                newName: "sales");

            migrationBuilder.RenameTable(
                name: "PasswordResetTokens",
                newName: "password_reset_tokens");

            migrationBuilder.RenameColumn(
                name: "SubTotal",
                table: "sales",
                newName: "subtotal");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "sales",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "sales",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "sales",
                newName: "total_amount");

            migrationBuilder.RenameColumn(
                name: "SaleDate",
                table: "sales",
                newName: "sale_date");

            migrationBuilder.RenameColumn(
                name: "PaymentMethod",
                table: "sales",
                newName: "payment_method");

            migrationBuilder.RenameColumn(
                name: "InvoiceNumber",
                table: "sales",
                newName: "invoice_number");

            migrationBuilder.RenameColumn(
                name: "InvoiceKey",
                table: "sales",
                newName: "invoice_key");

            migrationBuilder.RenameColumn(
                name: "EstablishmentId",
                table: "sales",
                newName: "establishment_id");

            migrationBuilder.RenameColumn(
                name: "DiscountAmount",
                table: "sales",
                newName: "discount_amount");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "sales",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CancellationReason",
                table: "sales",
                newName: "cancellation_reason");

            migrationBuilder.RenameColumn(
                name: "SoldByEmployeeId",
                table: "sales",
                newName: "created_by_employee_id");

            migrationBuilder.RenameColumn(
                name: "SaleNumber",
                table: "sales",
                newName: "code");

            migrationBuilder.RenameColumn(
                name: "PrescriptionNumber",
                table: "sales",
                newName: "payment_status");

            migrationBuilder.RenameColumn(
                name: "PrescriptionDate",
                table: "sales",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "PrescriberRegistration",
                table: "sales",
                newName: "invoice_status");

            migrationBuilder.RenameColumn(
                name: "CanceledAt",
                table: "sales",
                newName: "payment_date");

            migrationBuilder.RenameColumn(
                name: "AuthorizedByPharmacistId",
                table: "sales",
                newName: "updated_by_employee_id");

            migrationBuilder.RenameIndex(
                name: "IX_Sales_SoldByEmployeeId",
                table: "sales",
                newName: "IX_sales_created_by_employee_id");

            migrationBuilder.RenameIndex(
                name: "IX_Sales_AuthorizedByPharmacistId",
                table: "sales",
                newName: "IX_sales_updated_by_employee_id");

            migrationBuilder.AddColumn<bool>(
                name: "IsUsed",
                table: "TwoFactorAuths",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UsedAt",
                table: "TwoFactorAuths",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "subtotal",
                table: "sales",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "sales",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "total_amount",
                table: "sales",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<string>(
                name: "payment_method",
                table: "sales",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "invoice_key",
                table: "sales",
                type: "character varying(44)",
                maxLength: 44,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_amount",
                table: "sales",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                table: "sales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cancelled_by_employee_id",
                table: "sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "change_amount",
                table: "sales",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_percentage",
                table: "sales",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "has_multiple_payments",
                table: "sales",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "observations",
                table: "sales",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "paid_amount",
                table: "sales",
                type: "numeric",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "RawMaterials",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "AllowedUsage",
                table: "RawMaterials",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BasePrice",
                table: "RawMaterials",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BulkDensity",
                table: "RawMaterials",
                type: "numeric(6,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "RawMaterials",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CorrectionFactor",
                table: "RawMaterials",
                type: "numeric(6,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DilutionFactor",
                table: "RawMaterials",
                type: "numeric(8,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Indications",
                table: "RawMaterials",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVirtual",
                table: "RawMaterials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LastKnownPrice",
                table: "RawMaterials",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPriceDate",
                table: "RawMaterials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LastPurchasePrice",
                table: "RawMaterials",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPurchasePriceDate",
                table: "RawMaterials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LossFactor",
                table: "RawMaterials",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ParticleSize",
                table: "RawMaterials",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhysicalState",
                table: "RawMaterials",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Popularity",
                table: "RawMaterials",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PriceSource",
                table: "RawMaterials",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "SpecificMarkup",
                table: "RawMaterials",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Synonyms",
                table: "RawMaterials",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TappedDensity",
                table: "RawMaterials",
                type: "numeric(6,4)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PrescriptionQuoteId",
                table: "ManipulationOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "ManipulationOrders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "ManipulationOrders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Establishments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Street",
                table: "Establishments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "Establishments",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "Establishments",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8);

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "Establishments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Neighborhood",
                table: "Establishments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Establishments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Cnpj",
                table: "Establishments",
                type: "character varying(14)",
                maxLength: 14,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(14)",
                oldMaxLength: 14);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Establishments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptingOrders",
                table: "Establishments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AverageDeliveryMinutes",
                table: "Establishments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "Establishments",
                type: "numeric(3,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "Establishments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryRadiusKm",
                table: "Establishments",
                type: "numeric(5,1)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FeaturesEnabled",
                table: "Establishments",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InscricaoEstadual",
                table: "Establishments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMarketplaceActive",
                table: "Establishments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Establishments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketplaceDescription",
                table: "Establishments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketplaceOpeningHours",
                table: "Establishments",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxEmployeesLimit",
                table: "Establishments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxOrdersLimit",
                table: "Establishments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinOrderAmount",
                table: "Establishments",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "StripeConnectAccountId",
                table: "Establishments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subscription_status",
                table: "Establishments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalRatings",
                table: "Establishments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAt",
                table: "Establishments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Street",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "employees",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Salary",
                table: "employees",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "employees",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8);

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Neighborhood",
                table: "employees",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "HireDate",
                table: "employees",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DateOfBirth",
                table: "employees",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<string>(
                name: "Cpf",
                table: "employees",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(11)",
                oldMaxLength: 11);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "employees",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AddColumn<string>(
                name: "crm",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "crm_state",
                table: "employees",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "AccessLevels",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AccessLevels",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                table: "password_reset_tokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_sales",
                table: "sales",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_password_reset_tokens",
                table: "password_reset_tokens",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "active_ingredients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    synonyms = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    subcategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    default_unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    min_dosage = table.Column<decimal>(type: "numeric", nullable: true),
                    max_dosage = table.Column<decimal>(type: "numeric", nullable: true),
                    price_per_unit = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    indications = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    requires_prescription = table.Column<bool>(type: "boolean", nullable: false),
                    is_controlled = table.Column<bool>(type: "boolean", nullable: false),
                    dcb_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    cas_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    popularity = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_active_ingredients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    EstablishmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "auditor_access_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    auditor_name = table.Column<string>(type: "text", nullable: false),
                    auditor_document = table.Column<string>(type: "text", nullable: false),
                    auditor_institution = table.Column<string>(type: "text", nullable: false),
                    auditor_credential = table.Column<string>(type: "text", nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    access_reason = table.Column<string>(type: "text", nullable: false),
                    requested_reports = table.Column<string[]>(type: "text[]", nullable: true),
                    approved_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    access_granted = table.Column<bool>(type: "boolean", nullable: false),
                    access_token = table.Column<string>(type: "text", nullable: true),
                    access_valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    access_valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_access_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    access_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditor_access_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "capsule_size_reference",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    size_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    volume_ml = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    capacity_mg_min = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    capacity_mg_max = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    practical_capacity_mg = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_common = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capsule_size_reference", x => x.id);
                    table.ForeignKey(
                        name: "FK_capsule_size_reference_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cart_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric", nullable: false),
                    requires_prescription = table.Column<bool>(type: "boolean", nullable: false),
                    is_controlled = table.Column<bool>(type: "boolean", nullable: false),
                    is_custom_formula = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cash_registers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    opening_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closing_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    opened_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    closed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    opening_balance = table.Column<decimal>(type: "numeric", nullable: false),
                    closing_balance = table.Column<decimal>(type: "numeric", nullable: true),
                    expected_balance = table.Column<decimal>(type: "numeric", nullable: true),
                    difference = table.Column<decimal>(type: "numeric", nullable: true),
                    total_sales = table.Column<decimal>(type: "numeric", nullable: false),
                    total_cash = table.Column<decimal>(type: "numeric", nullable: false),
                    total_card = table.Column<decimal>(type: "numeric", nullable: false),
                    total_pix = table.Column<decimal>(type: "numeric", nullable: false),
                    sales_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    observations = table.Column<string>(type: "text", nullable: true),
                    total_debit = table.Column<decimal>(type: "numeric", nullable: false),
                    total_credit = table.Column<decimal>(type: "numeric", nullable: false),
                    total_boleto = table.Column<decimal>(type: "numeric", nullable: false),
                    total_other = table.Column<decimal>(type: "numeric", nullable: false),
                    total_withdrawals = table.Column<decimal>(type: "numeric", nullable: false),
                    total_supplies = table.Column<decimal>(type: "numeric", nullable: false),
                    total_cancellations = table.Column<decimal>(type: "numeric", nullable: false),
                    closing_observations = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_registers", x => x.id);
                    table.ForeignKey(
                        name: "FK_cash_registers_employees_closed_by_employee_id",
                        column: x => x.closed_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_cash_registers_employees_opened_by_employee_id",
                        column: x => x.opened_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatalogCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EstablishmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IconClass = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogCategories_Establishments_EstablishmentId",
                        column: x => x.EstablishmentId,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    razao_social = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nome_fantasia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cnpj = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: false),
                    inscricao_estadual = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    inscricao_municipal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    cep = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    celular = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    website = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    alvara_sanitario = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    alvara_validade = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    autorizacao_anvisa = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    autorizacao_especial = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    logo_base64 = table.Column<string>(type: "text", nullable: true),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    prazo_validade_padrao_dias = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "controlled_inventory_checks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_month = table.Column<int>(type: "integer", nullable: false),
                    reference_year = table.Column<int>(type: "integer", nullable: false),
                    check_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    performed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    witnessed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pharmacist_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    total_items_checked = table.Column<int>(type: "integer", nullable: false),
                    items_ok = table.Column<int>(type: "integer", nullable: false),
                    items_with_divergence = table.Column<int>(type: "integer", nullable: false),
                    observations = table.Column<string>(type: "text", nullable: true),
                    corrective_actions = table.Column<string>(type: "text", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_controlled_inventory_checks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "controlled_substance_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raw_material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    balance_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    controlled_list = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    substance_dcb_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    substance_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    initial_balance = table.Column<decimal>(type: "numeric", nullable: false),
                    total_entries = table.Column<decimal>(type: "numeric", nullable: false),
                    total_exits = table.Column<decimal>(type: "numeric", nullable: false),
                    total_losses = table.Column<decimal>(type: "numeric", nullable: false),
                    total_adjustments = table.Column<decimal>(type: "numeric", nullable: false),
                    final_balance = table.Column<decimal>(type: "numeric", nullable: false),
                    physical_balance = table.Column<decimal>(type: "numeric", nullable: true),
                    difference = table.Column<decimal>(type: "numeric", nullable: true),
                    unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sngpc_sent = table.Column<bool>(type: "boolean", nullable: false),
                    sngpc_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sngpc_protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    observations = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_controlled_substance_balances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "controlled_substance_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raw_material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    movement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    controlled_list = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    substance_dcb_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    substance_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    balance_before = table.Column<decimal>(type: "numeric", nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    prescription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    manipulation_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    prescription_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    prescription_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    doctor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    doctor_crm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    patient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    patient_cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    invoice_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    invoice_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sngpc_sent = table.Column<bool>(type: "boolean", nullable: false),
                    sngpc_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sngpc_protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    sngpc_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    observations = table.Column<string>(type: "text", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    authorized_by_pharmacist_id = table.Column<Guid>(type: "uuid", nullable: true),
                    authorized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_controlled_substance_movements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EstablishmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DiscountType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PERCENTAGE"),
                    DiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    DiscountValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    MinOrderValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    MaxDiscountValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxUses = table.Column<int>(type: "integer", nullable: true),
                    MaxUsesPerCustomer = table.Column<int>(type: "integer", nullable: true),
                    UsedCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FirstPurchaseOnly = table.Column<bool>(type: "boolean", nullable: false),
                    ApplicableCategories = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coupons_Establishments_EstablishmentId",
                        column: x => x.EstablishmentId,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Coupons_employees_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Coupons_employees_UpdatedByEmployeeId",
                        column: x => x.UpdatedByEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    rg = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    birth_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gender = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    whatsapp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    zip_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    complement = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    neighborhood = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    state = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    allergies = table.Column<string>(type: "text", nullable: true),
                    medical_conditions = table.Column<string>(type: "text", nullable: true),
                    observations = table.Column<string>(type: "text", nullable: true),
                    consent_data_processing = table.Column<bool>(type: "boolean", nullable: false),
                    consent_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    block_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_latitude = table.Column<double>(type: "double precision", nullable: true),
                    default_longitude = table.Column<double>(type: "double precision", nullable: true),
                    profile_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    firebase_fcm_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    preferred_payment_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    login_provider = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    external_provider_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                    table.ForeignKey(
                        name: "FK_customers_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customers_employees_created_by_employee_id",
                        column: x => x.created_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customers_employees_updated_by_employee_id",
                        column: x => x.updated_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "dual_verifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manipulation_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    verification_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    first_verifier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_verification_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    first_verifier_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    first_verifier_signature = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    second_verifier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    second_verification_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    second_verifier_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    second_verifier_signature = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    approved = table.Column<bool>(type: "boolean", nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    checklist_json = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dual_verifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_dual_verifications_ManipulationOrders_manipulation_order_id",
                        column: x => x.manipulation_order_id,
                        principalTable: "ManipulationOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dual_verifications_employees_first_verifier_id",
                        column: x => x.first_verifier_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dual_verifications_employees_second_verifier_id",
                        column: x => x.second_verifier_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "establishment_pricing_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    fee_1_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fee_1_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    fee_2_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fee_2_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    fee_3_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fee_3_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    markup_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    packaging_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    manipulation_fee = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    quote_validity_days = table.Column<int>(type: "integer", nullable: true),
                    inflation_rate_monthly = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    safety_margin_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    apply_minimum_price = table.Column<bool>(type: "boolean", nullable: false),
                    round_to_cents = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_establishment_pricing_config", x => x.id);
                    table.ForeignKey(
                        name: "FK_establishment_pricing_config_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_establishment_pricing_config_employees_updated_by_employee_~",
                        column: x => x.updated_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "establishment_pricing_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inflation_rate_monthly = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0.5m),
                    safety_margin_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 10m),
                    default_profit_margin = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 100m),
                    price_validity_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 180),
                    manipulation_fee = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 25m),
                    default_packaging_cost = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 5m),
                    alert_on_estimated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    block_without_stock = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_establishment_pricing_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_establishment_pricing_settings_Establishments_establishment~",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EstablishmentQRCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EstablishmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ScanCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastScannedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstablishmentQRCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstablishmentQRCodes_Establishments_EstablishmentId",
                        column: x => x.EstablishmentId,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EstablishmentQRCodes_employees_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inscricao_estadual = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    inscricao_municipal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    certificate_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    certificate_password = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    certificate_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    certificate_serial = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    environment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "HOMOLOGACAO"),
                    uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    nfe_series = table.Column<int>(type: "integer", nullable: false),
                    nfe_last_number = table.Column<int>(type: "integer", nullable: false),
                    nfce_series = table.Column<int>(type: "integer", nullable: false),
                    nfce_last_number = table.Column<int>(type: "integer", nullable: false),
                    csc_id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    csc_token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tax_regime = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "SIMPLES_NACIONAL"),
                    default_cfop_venda = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    default_cfop_manipulacao = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    default_ncm_manipulacao = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_api_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    provider_api_secret = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contingency_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    contingency_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    print_danfe_auto = table.Column<bool>(type: "boolean", nullable: false),
                    danfe_logo_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    default_nature = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    default_additional_info = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_configs", x => x.id);
                    table.ForeignKey(
                        name: "FK_fiscal_configs_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_number = table.Column<int>(type: "integer", nullable: false),
                    series = table.Column<int>(type: "integer", nullable: false),
                    invoice_key = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    invoice_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "NFCE"),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    authorization_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDENTE"),
                    cancellation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xml_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    pdf_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_invoices", x => x.id);
                    table.ForeignKey(
                        name: "FK_fiscal_invoices_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fiscal_invoices_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    request_xml = table.Column<string>(type: "text", nullable: true),
                    response_xml = table.Column<string>(type: "text", nullable: true),
                    status_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    status_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_number_gaps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    series = table.Column<int>(type: "integer", nullable: false),
                    start_number = table.Column<int>(type: "integer", nullable: false),
                    end_number = table.Column<int>(type: "integer", nullable: false),
                    justification = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDENTE"),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_number_gaps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "label_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    template_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    pharmaceutical_form = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    width = table.Column<decimal>(type: "numeric", nullable: false),
                    height = table.Column<decimal>(type: "numeric", nullable: false),
                    html_template = table.Column<string>(type: "text", nullable: false),
                    css_styles = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    include_establishment_name = table.Column<bool>(type: "boolean", nullable: false),
                    include_pharmacist_name = table.Column<bool>(type: "boolean", nullable: false),
                    include_formula_name = table.Column<bool>(type: "boolean", nullable: false),
                    include_composition = table.Column<bool>(type: "boolean", nullable: false),
                    include_posology = table.Column<bool>(type: "boolean", nullable: false),
                    include_validity = table.Column<bool>(type: "boolean", nullable: false),
                    include_batch_number = table.Column<bool>(type: "boolean", nullable: false),
                    include_manipulation_date = table.Column<bool>(type: "boolean", nullable: false),
                    include_patient_name = table.Column<bool>(type: "boolean", nullable: false),
                    include_qr_code = table.Column<bool>(type: "boolean", nullable: false),
                    include_warnings = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_label_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_label_templates_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_label_templates_employees_created_by_employee_id",
                        column: x => x.created_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_label_templates_employees_updated_by_employee_id",
                        column: x => x.updated_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "manipulation_leftovers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manipulation_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raw_material_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    destination = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    destination_details = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    registered_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    batch_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reintegrated_to_stock = table.Column<bool>(type: "boolean", nullable: false),
                    reintegration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manipulation_leftovers", x => x.id);
                    table.ForeignKey(
                        name: "FK_manipulation_leftovers_ManipulationOrders_manipulation_orde~",
                        column: x => x.manipulation_order_id,
                        principalTable: "ManipulationOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_manipulation_leftovers_RawMaterials_raw_material_id",
                        column: x => x.raw_material_id,
                        principalTable: "RawMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_manipulation_leftovers_employees_registered_by_employee_id",
                        column: x => x.registered_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "manipulation_losses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manipulation_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raw_material_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    loss_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    registered_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    batch_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    value_lost = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manipulation_losses", x => x.id);
                    table.ForeignKey(
                        name: "FK_manipulation_losses_ManipulationOrders_manipulation_order_id",
                        column: x => x.manipulation_order_id,
                        principalTable: "ManipulationOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_manipulation_losses_RawMaterials_raw_material_id",
                        column: x => x.raw_material_id,
                        principalTable: "RawMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_manipulation_losses_employees_registered_by_employee_id",
                        column: x => x.registered_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "manipulation_order_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manipulation_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raw_material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    required_quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    weighed_quantity = table.Column<decimal>(type: "numeric", nullable: true),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric", nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    weighed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    weighed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    checked_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    observations = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manipulation_order_components", x => x.id);
                    table.ForeignKey(
                        name: "FK_manipulation_order_components_Batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "Batches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_manipulation_order_components_ManipulationOrders_manipulati~",
                        column: x => x.manipulation_order_id,
                        principalTable: "ManipulationOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_manipulation_order_components_RawMaterials_raw_material_id",
                        column: x => x.raw_material_id,
                        principalTable: "RawMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManipulationSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ManipulationOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PerformedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CheckedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PassedIntermediateCheck = table.Column<bool>(type: "boolean", nullable: true),
                    Observations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StepData = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ManipulationOrderId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManipulationSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManipulationSteps_ManipulationOrders_ManipulationOrderId",
                        column: x => x.ManipulationOrderId,
                        principalTable: "ManipulationOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManipulationSteps_ManipulationOrders_ManipulationOrderId1",
                        column: x => x.ManipulationOrderId1,
                        principalTable: "ManipulationOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManipulationSteps_employees_CheckedByEmployeeId",
                        column: x => x.CheckedByEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManipulationSteps_employees_PerformedByEmployeeId",
                        column: x => x.PerformedByEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stripe_payment_method_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    external_payment_method_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    gateway_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    card_brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    card_last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_methods", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_methods_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pharmaceutical_forms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_system_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_custom = table.Column<bool>(type: "boolean", nullable: false),
                    minimum_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    max_quantity_limit = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    default_validity_days = table.Column<int>(type: "integer", nullable: false),
                    default_unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    preparation_time_hours = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    usage_instructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    usage_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pharmaceutical_forms", x => x.id);
                    table.ForeignKey(
                        name: "FK_pharmaceutical_forms_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pharmaceutical_forms_employees_created_by_employee_id",
                        column: x => x.created_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_pharmaceutical_forms_employees_updated_by_employee_id",
                        column: x => x.updated_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "pharmacist_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manipulation_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pharmacist_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pharmacist_name = table.Column<string>(type: "text", nullable: false),
                    pharmacist_crf = table.Column<string>(type: "text", nullable: false),
                    pharmacist_crf_state = table.Column<string>(type: "text", nullable: false),
                    approval_type = table.Column<string>(type: "text", nullable: false),
                    approval_status = table.Column<string>(type: "text", nullable: false),
                    prescription_valid = table.Column<bool>(type: "boolean", nullable: false),
                    prescription_within_validity = table.Column<bool>(type: "boolean", nullable: false),
                    dose_within_limits = table.Column<bool>(type: "boolean", nullable: false),
                    no_interactions_detected = table.Column<bool>(type: "boolean", nullable: false),
                    patient_data_complete = table.Column<bool>(type: "boolean", nullable: false),
                    controlled_list_verified = table.Column<string>(type: "text", nullable: true),
                    observations = table.Column<string>(type: "text", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    digital_signature = table.Column<string>(type: "text", nullable: true),
                    record_hash = table.Column<string>(type: "text", nullable: true),
                    previous_record_hash = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pharmacist_approvals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_payout_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stripe_connect_account_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    agency_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    account_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    pix_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pharmacy_payout_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_pharmacy_payout_accounts_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "platform_commissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    week_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_sales_count = table.Column<int>(type: "integer", nullable: false),
                    commission_rate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    total_sales_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_commission_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_commissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_platform_commissions_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_types",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PharmaceuticalForm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "production_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manipulation_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    produced_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    verified_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by_pharmacist_id = table.Column<Guid>(type: "uuid", nullable: true),
                    batch_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    production_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    quantity_produced = table.Column<decimal>(type: "numeric", nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    temperature = table.Column<decimal>(type: "numeric", nullable: true),
                    humidity = table.Column<decimal>(type: "numeric", nullable: true),
                    environmental_conditions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    equipment_used = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    observations = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    quality_check_passed = table.Column<bool>(type: "boolean", nullable: false),
                    quality_check_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expected_quantity = table.Column<decimal>(type: "numeric", nullable: true),
                    actual_quantity = table.Column<decimal>(type: "numeric", nullable: true),
                    yield_percentage = table.Column<decimal>(type: "numeric", nullable: true),
                    is_yield_acceptable = table.Column<bool>(type: "boolean", nullable: false),
                    yield_deviation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    production_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    production_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_production_time_minutes = table.Column<int>(type: "integer", nullable: true),
                    quality_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_production_records_ManipulationOrders_manipulation_order_id",
                        column: x => x.manipulation_order_id,
                        principalTable: "ManipulationOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_production_records_employees_approved_by_pharmacist_id",
                        column: x => x.approved_by_pharmacist_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_records_employees_produced_by_employee_id",
                        column: x => x.produced_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_records_employees_verified_by_employee_id",
                        column: x => x.verified_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saas_admins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    password_algorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_admins", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sale_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    cash_received = table.Column<decimal>(type: "numeric", nullable: true),
                    change_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    card_brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    card_last_digits = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    installments = table.Column<int>(type: "integer", nullable: true),
                    nsu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    authorization_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pix_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    pix_transaction_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    pix_qr_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    boleto_barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    boleto_due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    boleto_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    gateway_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    gateway_transaction_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    gateway_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    gateway_response = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    payment_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    observations = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_sale_payments_employees_processed_by_employee_id",
                        column: x => x.processed_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sale_payments_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "special_prescription_controls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prescription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    prescription_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prescription_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    prescription_series = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    validity_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    doctor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    doctor_crm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    doctor_crm_state = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    patient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    patient_document = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    patient_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    patient_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    patient_state = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    medication = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    posology = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    retained = table.Column<bool>(type: "boolean", nullable: false),
                    retention_reason = table.Column<string>(type: "text", nullable: true),
                    observations = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_special_prescription_controls", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    mercadopago_plan_id_monthly = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    mercadopago_plan_id_yearly = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    abacatepay_plan_id_monthly = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    abacatepay_plan_id_yearly = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    price_monthly = table.Column<decimal>(type: "numeric", nullable: false),
                    price_yearly = table.Column<decimal>(type: "numeric", nullable: false),
                    max_employees = table.Column<int>(type: "integer", nullable: true),
                    max_monthly_orders = table.Column<int>(type: "integer", nullable: true),
                    features = table.Column<string>(type: "text", nullable: false),
                    stripe_price_id_monthly = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    stripe_price_id_yearly = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supplier_certifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    certification_type = table.Column<string>(type: "text", nullable: false),
                    certification_number = table.Column<string>(type: "text", nullable: true),
                    issuing_authority = table.Column<string>(type: "text", nullable: true),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    document_path = table.Column<string>(type: "text", nullable: true),
                    document_hash = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    verified_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    allows_controlled_substances = table.Column<bool>(type: "boolean", nullable: false),
                    controlled_lists_allowed = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_certifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_supplier_certifications_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_quality_scores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluation_period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    evaluation_period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_deliveries = table.Column<int>(type: "integer", nullable: false),
                    on_time_deliveries = table.Column<int>(type: "integer", nullable: false),
                    total_batches_received = table.Column<int>(type: "integer", nullable: false),
                    batches_approved = table.Column<int>(type: "integer", nullable: false),
                    batches_rejected = table.Column<int>(type: "integer", nullable: false),
                    non_conformities_count = table.Column<int>(type: "integer", nullable: false),
                    delivery_score = table.Column<decimal>(type: "numeric", nullable: true),
                    quality_score = table.Column<decimal>(type: "numeric", nullable: true),
                    documentation_score = table.Column<decimal>(type: "numeric", nullable: true),
                    overall_score = table.Column<decimal>(type: "numeric", nullable: true),
                    classification = table.Column<string>(type: "text", nullable: true),
                    calculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_quality_scores", x => x.id);
                    table.ForeignKey(
                        name: "FK_supplier_quality_scores_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auditor_access_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auditor_access_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "text", nullable: false),
                    action_details = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditor_access_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_auditor_access_logs_auditor_access_requests_auditor_access_~",
                        column: x => x.auditor_access_id,
                        principalTable: "auditor_access_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cash_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cash_register_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_movements", x => x.id);
                    table.ForeignKey(
                        name: "FK_cash_movements_cash_registers_cash_register_id",
                        column: x => x.cash_register_id,
                        principalTable: "cash_registers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cash_movements_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatalogProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EstablishmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Composition = table.Column<string>(type: "text", nullable: true),
                    Dosage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PromotionalPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    PromotionEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsHighlight = table.Column<bool>(type: "boolean", nullable: false),
                    IsBestSeller = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    average_rating = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    TotalRatings = table.Column<int>(type: "integer", nullable: false),
                    TotalSold = table.Column<int>(type: "integer", nullable: false),
                    IsMarketplaceVisible = table.Column<bool>(type: "boolean", nullable: false),
                    SearchKeywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogProducts_CatalogCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CatalogCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CatalogProducts_Establishments_EstablishmentId",
                        column: x => x.EstablishmentId,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "controlled_inventory_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_check_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raw_material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    controlled_list = table.Column<string>(type: "text", nullable: false),
                    system_balance = table.Column<decimal>(type: "numeric", nullable: false),
                    physical_balance = table.Column<decimal>(type: "numeric", nullable: false),
                    unit = table.Column<string>(type: "text", nullable: false),
                    divergence_justified = table.Column<bool>(type: "boolean", nullable: false),
                    justification = table.Column<string>(type: "text", nullable: true),
                    photo_evidence_path = table.Column<string>(type: "text", nullable: true),
                    adjustment_made = table.Column<bool>(type: "boolean", nullable: false),
                    adjustment_movement_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_controlled_inventory_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_controlled_inventory_items_controlled_inventory_checks_inve~",
                        column: x => x.inventory_check_id,
                        principalTable: "controlled_inventory_checks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "carts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carts", x => x.id);
                    table.ForeignKey(
                        name: "FK_carts_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_carts_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CouponUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CouponId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiscountApplied = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CouponUsages_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "Coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CouponUsages_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    complement = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    neighborhood = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    state = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    zip_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_addresses", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_addresses_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    platform = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    device_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    os_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    app_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_devices", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_devices_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAuths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PasswordAlgorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    VerificationCodeExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastVerificationSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAuths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAuths_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerCarts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstablishmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerCarts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerCarts_Establishments_EstablishmentId",
                        column: x => x.EstablishmentId,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerCarts_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnlineOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstablishmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SaleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DeliveryFee = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PaymentStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DeliveryType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeliveryAddress = table.Column<string>(type: "text", nullable: true),
                    CustomerNotes = table.Column<string>(type: "text", nullable: true),
                    EstimatedReadyAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadyAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    platform_commission_rate = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    platform_commission_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    net_amount_to_pharmacy = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    stripe_payment_intent_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    delivery_latitude = table.Column<double>(type: "double precision", nullable: true),
                    delivery_longitude = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnlineOrders_Establishments_EstablishmentId",
                        column: x => x.EstablishmentId,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnlineOrders_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnlineOrders_sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "sales",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "prescription_quotes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    public_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    prescription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    prescription_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    customer_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    doctor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    doctor_crm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    doctor_crm_state = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    doctor_specialty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    usage_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pharmaceutical_form = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    total_quantity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total_quantity_numeric = table.Column<decimal>(type: "numeric", nullable: false),
                    total_quantity_unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    instructions = table.Column<string>(type: "text", nullable: true),
                    components_json = table.Column<string>(type: "jsonb", nullable: false),
                    materials_cost = table.Column<decimal>(type: "numeric", nullable: false),
                    markup_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    markup_value = table.Column<decimal>(type: "numeric", nullable: false),
                    labor_cost = table.Column<decimal>(type: "numeric", nullable: false),
                    packaging_cost = table.Column<decimal>(type: "numeric", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric", nullable: false),
                    final_price = table.Column<decimal>(type: "numeric", nullable: false),
                    estimated_days = table.Column<int>(type: "integer", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_ip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    customer_observations = table.Column<string>(type: "text", nullable: true),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    manipulation_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    whatsapp_sent = table.Column<bool>(type: "boolean", nullable: false),
                    whatsapp_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    whatsapp_message_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    email_sent = table.Column<bool>(type: "boolean", nullable: false),
                    email_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    email_sent_to = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    internal_notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    view_count = table.Column<int>(type: "integer", nullable: false),
                    last_viewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prescription_quotes", x => x.id);
                    table.ForeignKey(
                        name: "FK_prescription_quotes_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prescription_quotes_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "prescriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prescription_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    doctor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    doctor_crm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    doctor_crm_state = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    prescription_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    controlled_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    prescription_color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    medications = table.Column<string>(type: "text", nullable: false),
                    posology = table.Column<string>(type: "text", nullable: false),
                    observations = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    image_path = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    validated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    validated_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    validation_notes = table.Column<string>(type: "text", nullable: true),
                    manipulation_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    manipulation_generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prescriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_prescriptions_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prescriptions_ManipulationOrders_manipulation_order_id",
                        column: x => x.manipulation_order_id,
                        principalTable: "ManipulationOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_prescriptions_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "search_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    search_term = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    result_count = table.Column<int>(type: "integer", nullable: false),
                    search_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_search_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_search_history_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_invoice_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    fiscal_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_number = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ncm = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    cfop = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    unit = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric", nullable: false),
                    discount = table.Column<decimal>(type: "numeric", nullable: false),
                    cst_icms = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    icms_base = table.Column<decimal>(type: "numeric", nullable: false),
                    icms_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    icms_value = table.Column<decimal>(type: "numeric", nullable: false),
                    cst_pis = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    pis_base = table.Column<decimal>(type: "numeric", nullable: false),
                    pis_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    pis_value = table.Column<decimal>(type: "numeric", nullable: false),
                    cst_cofins = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    cofins_base = table.Column<decimal>(type: "numeric", nullable: false),
                    cofins_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    cofins_value = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_invoice_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_fiscal_invoice_items_fiscal_invoices_fiscal_invoice_id",
                        column: x => x.fiscal_invoice_id,
                        principalTable: "fiscal_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_queue",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDENTE"),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    last_attempt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_attempt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    fiscal_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_queue", x => x.id);
                    table.ForeignKey(
                        name: "FK_fiscal_queue_fiscal_invoices_fiscal_invoice_id",
                        column: x => x.fiscal_invoice_id,
                        principalTable: "fiscal_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_fiscal_queue_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "generated_labels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    manipulation_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    patient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    prescriber_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    prescriber_registration = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    formula_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    pharmaceutical_form = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    composition = table.Column<string>(type: "text", nullable: true),
                    composition_json = table.Column<string>(type: "jsonb", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric", nullable: true),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    batch_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    manipulation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    posology = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    administration_route = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    storage_conditions = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    storage_instructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    warnings = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    usage_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_controlled = table.Column<bool>(type: "boolean", nullable: false),
                    control_schedule = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    pharmacy_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    pharmacy_cnpj = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    pharmacy_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    pharmacy_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    pharmacist_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    pharmacist_crf = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    pharmacist_crm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    qr_code_data = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    qr_code_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    barcode_data = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    generated_html = table.Column<string>(type: "text", nullable: true),
                    print_count = table.Column<int>(type: "integer", nullable: false),
                    last_printed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_printed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_labels", x => x.id);
                    table.ForeignKey(
                        name: "FK_generated_labels_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_generated_labels_ManipulationOrders_manipulation_order_id",
                        column: x => x.manipulation_order_id,
                        principalTable: "ManipulationOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_generated_labels_employees_created_by_employee_id",
                        column: x => x.created_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_generated_labels_employees_last_printed_by_id",
                        column: x => x.last_printed_by_id,
                        principalTable: "employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_generated_labels_label_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "label_templates",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ManipulationPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ManipulationOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManipulationStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    StepType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CapturedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ManipulationOrderId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManipulationPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManipulationPhotos_ManipulationOrders_ManipulationOrderId",
                        column: x => x.ManipulationOrderId,
                        principalTable: "ManipulationOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManipulationPhotos_ManipulationOrders_ManipulationOrderId1",
                        column: x => x.ManipulationOrderId1,
                        principalTable: "ManipulationOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManipulationPhotos_ManipulationSteps_ManipulationStepId",
                        column: x => x.ManipulationStepId,
                        principalTable: "ManipulationSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManipulationPhotos_employees_CapturedByEmployeeId",
                        column: x => x.CapturedByEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pharmaceutical_form_subtypes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pharmaceutical_form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    minimum_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    base_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    yield_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    yield_unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    validity_days = table.Column<int>(type: "integer", nullable: true),
                    max_quantity_limit = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    capsule_size = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    capsule_volume_ml = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    capsule_capacity_mg_min = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    capsule_capacity_mg_max = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    capsule_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    preparation_instructions = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pharmaceutical_form_subtypes", x => x.id);
                    table.ForeignKey(
                        name: "FK_pharmaceutical_form_subtypes_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pharmaceutical_form_subtypes_employees_created_by_employee_~",
                        column: x => x.created_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_pharmaceutical_form_subtypes_employees_updated_by_employee_~",
                        column: x => x.updated_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_pharmaceutical_form_subtypes_pharmaceutical_forms_pharmaceu~",
                        column: x => x.pharmaceutical_form_id,
                        principalTable: "pharmaceutical_forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_sub_types",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BaseFormulaId = table.Column<Guid>(type: "uuid", nullable: true),
                    StandardQuantity = table.Column<decimal>(type: "numeric", nullable: true),
                    StandardUnit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    PriceModifier = table.Column<decimal>(type: "numeric", nullable: false),
                    ManipulationCostBase = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_sub_types", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_sub_types_product_types_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "product_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_gateway_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gateway_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    environment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    public_key_encrypted = table.Column<string>(type: "text", nullable: true),
                    secret_key_encrypted = table.Column<string>(type: "text", nullable: true),
                    webhook_secret_encrypted = table.Column<string>(type: "text", nullable: true),
                    access_token_encrypted = table.Column<string>(type: "text", nullable: true),
                    client_id_encrypted = table.Column<string>(type: "text", nullable: true),
                    client_secret_encrypted = table.Column<string>(type: "text", nullable: true),
                    api_base_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    webhook_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    last_tested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_test_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    last_test_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    additional_settings = table.Column<string>(type: "text", nullable: true),
                    supported_currencies = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_gateway_configs", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_gateway_configs_saas_admins_created_by_admin_id",
                        column: x => x.created_by_admin_id,
                        principalTable: "saas_admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_payment_gateway_configs_saas_admins_updated_by_admin_id",
                        column: x => x.updated_by_admin_id,
                        principalTable: "saas_admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "saas_admin_password_resets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    saas_admin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_admin_password_resets", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_admin_password_resets_saas_admins_saas_admin_id",
                        column: x => x.saas_admin_id,
                        principalTable: "saas_admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saas_admin_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    saas_admin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saas_admin_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_saas_admin_sessions_saas_admins_saas_admin_id",
                        column: x => x.saas_admin_id,
                        principalTable: "saas_admins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sale_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manipulation_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    prescription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    formula_id = table.Column<Guid>(type: "uuid", nullable: true),
                    catalog_product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    raw_material_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric", nullable: false),
                    cost_price = table.Column<decimal>(type: "numeric", nullable: false),
                    profit_margin = table.Column<decimal>(type: "numeric", nullable: false),
                    control_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    prescription_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    observations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_sale_items_CatalogProducts_catalog_product_id",
                        column: x => x.catalog_product_id,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_sale_items_ManipulationOrders_manipulation_order_id",
                        column: x => x.manipulation_order_id,
                        principalTable: "ManipulationOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_sale_items_RawMaterials_raw_material_id",
                        column: x => x.raw_material_id,
                        principalTable: "RawMaterials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_sale_items_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerAuthId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LogoutAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentEstablishmentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerSessions_CustomerAuths_CustomerAuthId",
                        column: x => x.CustomerAuthId,
                        principalTable: "CustomerAuths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerSessions_Establishments_CurrentEstablishmentId",
                        column: x => x.CurrentEstablishmentId,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerSessions_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "delivery_estimates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estimated_minutes = table.Column<int>(type: "integer", nullable: false),
                    estimated_delivery_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actual_delivery_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delivery_estimates", x => x.id);
                    table.ForeignKey(
                        name: "FK_delivery_estimates_OnlineOrders_order_id",
                        column: x => x.order_id,
                        principalTable: "OnlineOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnlineOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnlineOrderItems_CatalogProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OnlineOrderItems_OnlineOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "OnlineOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_ratings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    pharmacy_response = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    pharmacy_responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pharmacy_ratings", x => x.id);
                    table.ForeignKey(
                        name: "FK_pharmacy_ratings_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pharmacy_ratings_OnlineOrders_order_id",
                        column: x => x.order_id,
                        principalTable: "OnlineOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_pharmacy_ratings_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "platform_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    commission_rate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    net_amount_to_pharmacy = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    stripe_payment_intent_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    stripe_transfer_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_platform_transactions_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_platform_transactions_OnlineOrders_order_id",
                        column: x => x.order_id,
                        principalTable: "OnlineOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_platform_transactions_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "product_ratings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_ratings", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_ratings_CatalogProducts_catalog_product_id",
                        column: x => x.catalog_product_id,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_ratings_OnlineOrders_order_id",
                        column: x => x.order_id,
                        principalTable: "OnlineOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_product_ratings_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prescription_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    prescription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_url = table.Column<string>(type: "text", nullable: true),
                    file_base64 = table.Column<string>(type: "text", nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    uploaded_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ocr_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ocr_processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ocr_result = table.Column<string>(type: "jsonb", nullable: true),
                    ocr_confidence = table.Column<decimal>(type: "numeric", nullable: true),
                    ocr_error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prescription_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_prescription_files_prescriptions_prescription_id",
                        column: x => x.prescription_id,
                        principalTable: "prescriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "label_print_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    generated_label_id = table.Column<Guid>(type: "uuid", nullable: false),
                    printed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    printed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    copies = table.Column<int>(type: "integer", nullable: false),
                    format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    printer_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    print_reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_label_print_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_label_print_logs_employees_printed_by_id",
                        column: x => x.printed_by_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_label_print_logs_generated_labels_generated_label_id",
                        column: x => x.generated_label_id,
                        principalTable: "generated_labels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pharmaceutical_form_compositions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subtype_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raw_material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(8,4)", nullable: true),
                    quantity_per_yield = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_qsp = table.Column<bool>(type: "boolean", nullable: false),
                    is_optional = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pharmaceutical_form_compositions", x => x.id);
                    table.ForeignKey(
                        name: "FK_pharmaceutical_form_compositions_RawMaterials_raw_material_~",
                        column: x => x.raw_material_id,
                        principalTable: "RawMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pharmaceutical_form_compositions_pharmaceutical_form_subtyp~",
                        column: x => x.subtype_id,
                        principalTable: "pharmaceutical_form_subtypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_formulas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    customer_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    customer_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    product_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_sub_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    additional_ingredients = table.Column<string>(type: "jsonb", nullable: true),
                    customer_notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    pharmacist_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pharmaceutical_analysis = table.Column<string>(type: "text", nullable: true),
                    analyzed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    adjustment_request = table.Column<string>(type: "text", nullable: true),
                    requires_prescription = table.Column<bool>(type: "boolean", nullable: false),
                    is_controlled_substance = table.Column<bool>(type: "boolean", nullable: false),
                    has_incompatibilities = table.Column<bool>(type: "boolean", nullable: false),
                    incompatibility_details = table.Column<string>(type: "text", nullable: true),
                    estimated_shelf_life_days = table.Column<int>(type: "integer", nullable: true),
                    estimated_price = table.Column<decimal>(type: "numeric", nullable: true),
                    final_price = table.Column<decimal>(type: "numeric", nullable: true),
                    discount_applied = table.Column<decimal>(type: "numeric", nullable: false),
                    prescription_quote_id = table.Column<Guid>(type: "uuid", nullable: true),
                    manipulation_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    online_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paid_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    requires_refund = table.Column<bool>(type: "boolean", nullable: false),
                    refunded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    refund_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    session_token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    session_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_formulas", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_formulas_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customer_formulas_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_customer_formulas_prescription_quotes_prescription_quote_id",
                        column: x => x.prescription_quote_id,
                        principalTable: "prescription_quotes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_customer_formulas_product_sub_types_product_sub_type_id",
                        column: x => x.product_sub_type_id,
                        principalTable: "product_sub_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customer_formulas_product_types_product_type_id",
                        column: x => x.product_type_id,
                        principalTable: "product_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_webhook_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gateway_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    gateway_config_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    external_event_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    payload = table.Column<string>(type: "text", nullable: true),
                    headers = table.Column<string>(type: "text", nullable: true),
                    signature = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    signature_valid = table.Column<bool>(type: "boolean", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    error_stack_trace = table.Column<string>(type: "text", nullable: true),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_time_ms = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_webhook_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_webhook_logs_payment_gateway_configs_gateway_config~",
                        column: x => x.gateway_config_id,
                        principalTable: "payment_gateway_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    establishment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stripe_subscription_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    stripe_customer_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    gateway_config_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_subscription_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    external_customer_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    billing_cycle = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    current_period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    trial_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    trial_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_at_period_end = table.Column<bool>(type: "boolean", nullable: false),
                    canceled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_subscriptions_Establishments_establishment_id",
                        column: x => x.establishment_id,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_subscriptions_payment_gateway_configs_gateway_config_id",
                        column: x => x.gateway_config_id,
                        principalTable: "payment_gateway_configs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscriptions_subscription_plans_subscription_plan_id",
                        column: x => x.subscription_plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerCartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CartId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CustomerFormulaId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerCartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerCartItems_CatalogProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerCartItems_CustomerCarts_CartId",
                        column: x => x.CartId,
                        principalTable: "CustomerCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerCartItems_customer_formulas_CustomerFormulaId",
                        column: x => x.CustomerFormulaId,
                        principalTable: "customer_formulas",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "formula_cart_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cart_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_formula_id = table.Column<Guid>(type: "uuid", nullable: true),
                    catalog_product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_formula_cart_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_formula_cart_items_CatalogProducts_catalog_product_id",
                        column: x => x.catalog_product_id,
                        principalTable: "CatalogProducts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_formula_cart_items_carts_cart_id",
                        column: x => x.cart_id,
                        principalTable: "carts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_formula_cart_items_customer_formulas_customer_formula_id",
                        column: x => x.customer_formula_id,
                        principalTable: "customer_formulas",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "PharmaceuticalAnalysisLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerFormulaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PharmacistId = table.Column<Guid>(type: "uuid", nullable: false),
                    PharmacistName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PharmacistCrf = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Analysis = table.Column<string>(type: "text", nullable: true),
                    SafetyCheck = table.Column<bool>(type: "boolean", nullable: false),
                    DosageCheck = table.Column<bool>(type: "boolean", nullable: false),
                    InteractionCheck = table.Column<bool>(type: "boolean", nullable: false),
                    StabilityCheck = table.Column<bool>(type: "boolean", nullable: false),
                    Observations = table.Column<string>(type: "text", nullable: true),
                    InternalNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PharmaceuticalAnalysisLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PharmaceuticalAnalysisLog_customer_formulas_CustomerFormula~",
                        column: x => x.CustomerFormulaId,
                        principalTable: "customer_formulas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerFormulaId = table.Column<Guid>(type: "uuid", nullable: false),
                    OnlineOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Refunds_customer_formulas_CustomerFormulaId",
                        column: x => x.CustomerFormulaId,
                        principalTable: "customer_formulas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscription_invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stripe_invoice_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    gateway_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    external_payment_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "PENDING"),
                    invoice_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    invoice_pdf_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_invoices", x => x.id);
                    table.ForeignKey(
                        name: "FK_subscription_invoices_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalTable: "subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000101"),
                columns: new[] { "Description", "Scope" },
                values: new object[] { "Permite visualizar informações de estoque", "Own" });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000102"),
                columns: new[] { "Action", "Description", "DisplayName", "ResourceAction" },
                values: new object[] { "write", "Permite adicionar e editar itens de estoque", "Editar Estoque", "inventory.write" });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000201"),
                columns: new[] { "Description", "DisplayName", "Scope" },
                values: new object[] { "Permite criar novas vendas", "Criar Vendas", "Own" });

            migrationBuilder.CreateIndex(
                name: "IX_TwoFactorAuths_EmployeeId",
                table: "TwoFactorAuths",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_cancelled_by_employee_id",
                table: "sales",
                column: "cancelled_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_customer_id",
                table: "sales",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_Category",
                table: "RawMaterials",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_IsVirtual",
                table: "RawMaterials",
                column: "IsVirtual");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_EstablishmentId",
                table: "PurchaseOrders",
                column: "EstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_client_onboarding_establishment_id",
                table: "client_onboarding",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_EmployeeId",
                table: "password_reset_tokens",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_active_ingredients_category",
                table: "active_ingredients",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_active_ingredients_normalized_name",
                table: "active_ingredients",
                column: "normalized_name");

            migrationBuilder.CreateIndex(
                name: "IX_active_ingredients_popularity",
                table: "active_ingredients",
                column: "popularity");

            migrationBuilder.CreateIndex(
                name: "IX_auditor_access_logs_auditor_access_id",
                table: "auditor_access_logs",
                column: "auditor_access_id");

            migrationBuilder.CreateIndex(
                name: "IX_auditor_access_logs_created_at",
                table: "auditor_access_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_auditor_access_requests_access_token",
                table: "auditor_access_requests",
                column: "access_token");

            migrationBuilder.CreateIndex(
                name: "IX_auditor_access_requests_establishment_id",
                table: "auditor_access_requests",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_capsule_size_reference_establishment_id_size_code",
                table: "capsule_size_reference",
                columns: new[] { "establishment_id", "size_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_customer_id",
                table: "cart_items",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_establishment_id",
                table: "cart_items",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_session_token",
                table: "cart_items",
                column: "session_token");

            migrationBuilder.CreateIndex(
                name: "IX_carts_customer_id",
                table: "carts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_carts_establishment_id",
                table: "carts",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_cash_register_id",
                table: "cash_movements",
                column: "cash_register_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_employee_id",
                table: "cash_movements",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_registers_closed_by_employee_id",
                table: "cash_registers",
                column: "closed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_registers_opened_by_employee_id",
                table: "cash_registers",
                column: "opened_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogCategories_EstablishmentId",
                table: "CatalogCategories",
                column: "EstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogCategories_IsActive",
                table: "CatalogCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_CategoryId",
                table: "CatalogProducts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_EstablishmentId",
                table: "CatalogProducts",
                column: "EstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProducts_IsActive",
                table: "CatalogProducts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_controlled_inventory_checks_establishment_id",
                table: "controlled_inventory_checks",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_controlled_inventory_checks_reference_year_reference_month",
                table: "controlled_inventory_checks",
                columns: new[] { "reference_year", "reference_month" });

            migrationBuilder.CreateIndex(
                name: "IX_controlled_inventory_items_inventory_check_id",
                table: "controlled_inventory_items",
                column: "inventory_check_id");

            migrationBuilder.CreateIndex(
                name: "IX_controlled_inventory_items_raw_material_id",
                table: "controlled_inventory_items",
                column: "raw_material_id");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code",
                table: "Coupons",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_CreatedByEmployeeId",
                table: "Coupons",
                column: "CreatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_EstablishmentId",
                table: "Coupons",
                column: "EstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_IsActive_ValidFrom_ValidUntil",
                table: "Coupons",
                columns: new[] { "IsActive", "ValidFrom", "ValidUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_UpdatedByEmployeeId",
                table: "Coupons",
                column: "UpdatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_CouponId",
                table: "CouponUsages",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_CouponId_CustomerId",
                table: "CouponUsages",
                columns: new[] { "CouponId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_CustomerId",
                table: "CouponUsages",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_UsedAt",
                table: "CouponUsages",
                column: "UsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_customer_id",
                table: "customer_addresses",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_devices_customer_id",
                table: "customer_devices",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_devices_device_token",
                table: "customer_devices",
                column: "device_token");

            migrationBuilder.CreateIndex(
                name: "IX_customer_formulas_code",
                table: "customer_formulas",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "IX_customer_formulas_customer_id",
                table: "customer_formulas",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_formulas_establishment_id",
                table: "customer_formulas",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_formulas_prescription_quote_id",
                table: "customer_formulas",
                column: "prescription_quote_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_formulas_product_sub_type_id",
                table: "customer_formulas",
                column: "product_sub_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_formulas_product_type_id",
                table: "customer_formulas",
                column: "product_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_formulas_status",
                table: "customer_formulas",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAuths_Cpf",
                table: "CustomerAuths",
                column: "Cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAuths_CustomerId",
                table: "CustomerAuths",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAuths_IsVerified",
                table: "CustomerAuths",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAuths_Phone",
                table: "CustomerAuths",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCartItems_CartId",
                table: "CustomerCartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCartItems_CustomerFormulaId",
                table: "CustomerCartItems",
                column: "CustomerFormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCartItems_ProductId",
                table: "CustomerCartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCarts_CustomerId_EstablishmentId",
                table: "CustomerCarts",
                columns: new[] { "CustomerId", "EstablishmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCarts_EstablishmentId",
                table: "CustomerCarts",
                column: "EstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_customers_created_by_employee_id",
                table: "customers",
                column: "created_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_customers_establishment_id",
                table: "customers",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_customers_updated_by_employee_id",
                table: "customers",
                column: "updated_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSessions_CurrentEstablishmentId",
                table: "CustomerSessions",
                column: "CurrentEstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSessions_CustomerAuthId",
                table: "CustomerSessions",
                column: "CustomerAuthId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSessions_CustomerId",
                table: "CustomerSessions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSessions_ExpiresAt",
                table: "CustomerSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSessions_IsActive",
                table: "CustomerSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSessions_SessionToken",
                table: "CustomerSessions",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_delivery_estimates_order_id",
                table: "delivery_estimates",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_estimates_status",
                table: "delivery_estimates",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_dual_verifications_first_verifier_id",
                table: "dual_verifications",
                column: "first_verifier_id");

            migrationBuilder.CreateIndex(
                name: "IX_dual_verifications_manipulation_order_id",
                table: "dual_verifications",
                column: "manipulation_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_dual_verifications_second_verifier_id",
                table: "dual_verifications",
                column: "second_verifier_id");

            migrationBuilder.CreateIndex(
                name: "IX_establishment_pricing_config_establishment_id",
                table: "establishment_pricing_config",
                column: "establishment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_establishment_pricing_config_updated_by_employee_id",
                table: "establishment_pricing_config",
                column: "updated_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "uq_establishment_pricing_settings",
                table: "establishment_pricing_settings",
                column: "establishment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstablishmentQRCodes_Code",
                table: "EstablishmentQRCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstablishmentQRCodes_CreatedByEmployeeId",
                table: "EstablishmentQRCodes",
                column: "CreatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EstablishmentQRCodes_EstablishmentId",
                table: "EstablishmentQRCodes",
                column: "EstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EstablishmentQRCodes_IsActive",
                table: "EstablishmentQRCodes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_configs_establishment",
                table: "fiscal_configs",
                column: "establishment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_invoice_items_invoice",
                table: "fiscal_invoice_items",
                column: "fiscal_invoice_id");

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_invoices_establishment_id",
                table: "fiscal_invoices",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_invoices_establishment_issue_date",
                table: "fiscal_invoices",
                columns: new[] { "establishment_id", "issue_date" });

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_invoices_invoice_key",
                table: "fiscal_invoices",
                column: "invoice_key",
                unique: true,
                filter: "invoice_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_invoices_issue_date",
                table: "fiscal_invoices",
                column: "issue_date");

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_invoices_number_series",
                table: "fiscal_invoices",
                columns: new[] { "establishment_id", "invoice_number", "series" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_invoices_sale_id",
                table: "fiscal_invoices",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_invoices_status",
                table: "fiscal_invoices",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_logs_date",
                table: "fiscal_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_logs_establishment",
                table: "fiscal_logs",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_logs_invoice",
                table: "fiscal_logs",
                column: "fiscal_invoice_id");

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_gaps_establishment",
                table: "fiscal_number_gaps",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_queue_establishment",
                table: "fiscal_queue",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_queue_next",
                table: "fiscal_queue",
                column: "next_attempt");

            migrationBuilder.CreateIndex(
                name: "idx_fiscal_queue_status",
                table: "fiscal_queue",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_queue_fiscal_invoice_id",
                table: "fiscal_queue",
                column: "fiscal_invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_queue_sale_id",
                table: "fiscal_queue",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "IX_formula_cart_items_cart_id",
                table: "formula_cart_items",
                column: "cart_id");

            migrationBuilder.CreateIndex(
                name: "IX_formula_cart_items_catalog_product_id",
                table: "formula_cart_items",
                column: "catalog_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_formula_cart_items_customer_formula_id",
                table: "formula_cart_items",
                column: "customer_formula_id");

            migrationBuilder.CreateIndex(
                name: "IX_generated_labels_created_by_employee_id",
                table: "generated_labels",
                column: "created_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_generated_labels_establishment_id",
                table: "generated_labels",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_generated_labels_last_printed_by_id",
                table: "generated_labels",
                column: "last_printed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_generated_labels_manipulation_order_id",
                table: "generated_labels",
                column: "manipulation_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_generated_labels_template_id",
                table: "generated_labels",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_label_print_logs_generated_label_id",
                table: "label_print_logs",
                column: "generated_label_id");

            migrationBuilder.CreateIndex(
                name: "IX_label_print_logs_printed_by_id",
                table: "label_print_logs",
                column: "printed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_label_templates_created_by_employee_id",
                table: "label_templates",
                column: "created_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_label_templates_establishment_id",
                table: "label_templates",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_label_templates_updated_by_employee_id",
                table: "label_templates",
                column: "updated_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_manipulation_leftovers_manipulation_order_id",
                table: "manipulation_leftovers",
                column: "manipulation_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_manipulation_leftovers_raw_material_id",
                table: "manipulation_leftovers",
                column: "raw_material_id");

            migrationBuilder.CreateIndex(
                name: "IX_manipulation_leftovers_registered_by_employee_id",
                table: "manipulation_leftovers",
                column: "registered_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_manipulation_losses_manipulation_order_id",
                table: "manipulation_losses",
                column: "manipulation_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_manipulation_losses_raw_material_id",
                table: "manipulation_losses",
                column: "raw_material_id");

            migrationBuilder.CreateIndex(
                name: "IX_manipulation_losses_registered_by_employee_id",
                table: "manipulation_losses",
                column: "registered_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_manipulation_order_components_batch_id",
                table: "manipulation_order_components",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_manipulation_order_components_manipulation_order_id",
                table: "manipulation_order_components",
                column: "manipulation_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_manipulation_order_components_raw_material_id",
                table: "manipulation_order_components",
                column: "raw_material_id");

            migrationBuilder.CreateIndex(
                name: "IX_ManipulationPhotos_CapturedByEmployeeId",
                table: "ManipulationPhotos",
                column: "CapturedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ManipulationPhotos_ManipulationOrderId",
                table: "ManipulationPhotos",
                column: "ManipulationOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ManipulationPhotos_ManipulationOrderId1",
                table: "ManipulationPhotos",
                column: "ManipulationOrderId1");

            migrationBuilder.CreateIndex(
                name: "IX_ManipulationPhotos_ManipulationStepId",
                table: "ManipulationPhotos",
                column: "ManipulationStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ManipulationSteps_CheckedByEmployeeId",
                table: "ManipulationSteps",
                column: "CheckedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ManipulationSteps_ManipulationOrderId",
                table: "ManipulationSteps",
                column: "ManipulationOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ManipulationSteps_ManipulationOrderId1",
                table: "ManipulationSteps",
                column: "ManipulationOrderId1");

            migrationBuilder.CreateIndex(
                name: "IX_ManipulationSteps_PerformedByEmployeeId",
                table: "ManipulationSteps",
                column: "PerformedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrderItems_OrderId",
                table: "OnlineOrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrderItems_ProductId",
                table: "OnlineOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrders_CustomerId",
                table: "OnlineOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrders_EstablishmentId",
                table: "OnlineOrders",
                column: "EstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrders_OrderNumber",
                table: "OnlineOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrders_SaleId",
                table: "OnlineOrders",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineOrders_Status",
                table: "OnlineOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_payment_gateway_configs_created_by_admin_id",
                table: "payment_gateway_configs",
                column: "created_by_admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_gateway_configs_environment",
                table: "payment_gateway_configs",
                column: "environment");

            migrationBuilder.CreateIndex(
                name: "IX_payment_gateway_configs_gateway_type",
                table: "payment_gateway_configs",
                column: "gateway_type");

            migrationBuilder.CreateIndex(
                name: "IX_payment_gateway_configs_is_active",
                table: "payment_gateway_configs",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_payment_gateway_configs_is_default",
                table: "payment_gateway_configs",
                column: "is_default");

            migrationBuilder.CreateIndex(
                name: "IX_payment_gateway_configs_updated_by_admin_id",
                table: "payment_gateway_configs",
                column: "updated_by_admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_methods_establishment_id",
                table: "payment_methods",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_webhook_logs_created_at",
                table: "payment_webhook_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_payment_webhook_logs_event_type",
                table: "payment_webhook_logs",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "IX_payment_webhook_logs_external_event_id",
                table: "payment_webhook_logs",
                column: "external_event_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_webhook_logs_gateway_config_id",
                table: "payment_webhook_logs",
                column: "gateway_config_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_webhook_logs_gateway_type",
                table: "payment_webhook_logs",
                column: "gateway_type");

            migrationBuilder.CreateIndex(
                name: "IX_payment_webhook_logs_status",
                table: "payment_webhook_logs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_payment_webhook_logs_subscription_id",
                table: "payment_webhook_logs",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmaceutical_form_compositions_raw_material_id",
                table: "pharmaceutical_form_compositions",
                column: "raw_material_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmaceutical_form_compositions_subtype_id",
                table: "pharmaceutical_form_compositions",
                column: "subtype_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmaceutical_form_compositions_subtype_id_raw_material_id",
                table: "pharmaceutical_form_compositions",
                columns: new[] { "subtype_id", "raw_material_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pharmaceutical_form_subtypes_created_by_employee_id",
                table: "pharmaceutical_form_subtypes",
                column: "created_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmaceutical_form_subtypes_establishment_id",
                table: "pharmaceutical_form_subtypes",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmaceutical_form_subtypes_establishment_id_pharmaceutica~",
                table: "pharmaceutical_form_subtypes",
                columns: new[] { "establishment_id", "pharmaceutical_form_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pharmaceutical_form_subtypes_pharmaceutical_form_id",
                table: "pharmaceutical_form_subtypes",
                column: "pharmaceutical_form_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmaceutical_form_subtypes_updated_by_employee_id",
                table: "pharmaceutical_form_subtypes",
                column: "updated_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmaceutical_forms_created_by_employee_id",
                table: "pharmaceutical_forms",
                column: "created_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmaceutical_forms_establishment_id_code",
                table: "pharmaceutical_forms",
                columns: new[] { "establishment_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pharmaceutical_forms_establishment_id_is_active",
                table: "pharmaceutical_forms",
                columns: new[] { "establishment_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_pharmaceutical_forms_updated_by_employee_id",
                table: "pharmaceutical_forms",
                column: "updated_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_PharmaceuticalAnalysisLog_CustomerFormulaId",
                table: "PharmaceuticalAnalysisLog",
                column: "CustomerFormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacist_approvals_approval_status",
                table: "pharmacist_approvals",
                column: "approval_status");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacist_approvals_created_at",
                table: "pharmacist_approvals",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacist_approvals_establishment_id",
                table: "pharmacist_approvals",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacist_approvals_manipulation_order_id",
                table: "pharmacist_approvals",
                column: "manipulation_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacist_approvals_pharmacist_employee_id",
                table: "pharmacist_approvals",
                column: "pharmacist_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacy_payout_accounts_establishment_id",
                table: "pharmacy_payout_accounts",
                column: "establishment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pharmacy_payout_accounts_stripe_connect_account_id",
                table: "pharmacy_payout_accounts",
                column: "stripe_connect_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacy_ratings_customer_id",
                table: "pharmacy_ratings",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacy_ratings_establishment_id",
                table: "pharmacy_ratings",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacy_ratings_order_id",
                table: "pharmacy_ratings",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_commissions_establishment_id_week_start_date",
                table: "platform_commissions",
                columns: new[] { "establishment_id", "week_start_date" });

            migrationBuilder.CreateIndex(
                name: "IX_platform_commissions_status",
                table: "platform_commissions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_platform_transactions_customer_id",
                table: "platform_transactions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_transactions_establishment_id",
                table: "platform_transactions",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_transactions_order_id_status",
                table: "platform_transactions",
                columns: new[] { "order_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_platform_transactions_stripe_payment_intent_id",
                table: "platform_transactions",
                column: "stripe_payment_intent_id");

            migrationBuilder.CreateIndex(
                name: "IX_prescription_files_prescription_id",
                table: "prescription_files",
                column: "prescription_id");

            migrationBuilder.CreateIndex(
                name: "IX_prescription_quotes_customer_id",
                table: "prescription_quotes",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_prescription_quotes_establishment_id",
                table: "prescription_quotes",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_customer_id",
                table: "prescriptions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_establishment_id",
                table: "prescriptions",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_manipulation_order_id",
                table: "prescriptions",
                column: "manipulation_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_ratings_catalog_product_id",
                table: "product_ratings",
                column: "catalog_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_ratings_customer_id",
                table: "product_ratings",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_ratings_order_id",
                table: "product_ratings",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_sub_types_ProductTypeId",
                table: "product_sub_types",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_product_types_Category",
                table: "product_types",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_product_types_IsActive",
                table: "product_types",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_production_records_approved_by_pharmacist_id",
                table: "production_records",
                column: "approved_by_pharmacist_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_records_manipulation_order_id",
                table: "production_records",
                column: "manipulation_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_records_produced_by_employee_id",
                table: "production_records",
                column: "produced_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_records_verified_by_employee_id",
                table: "production_records",
                column: "verified_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_CustomerFormulaId",
                table: "Refunds",
                column: "CustomerFormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_saas_admin_password_resets_expires_at",
                table: "saas_admin_password_resets",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_saas_admin_password_resets_saas_admin_id",
                table: "saas_admin_password_resets",
                column: "saas_admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_saas_admin_password_resets_token",
                table: "saas_admin_password_resets",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saas_admin_sessions_saas_admin_id",
                table: "saas_admin_sessions",
                column: "saas_admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_saas_admin_sessions_token",
                table: "saas_admin_sessions",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saas_admins_email",
                table: "saas_admins",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sale_items_catalog_product_id",
                table: "sale_items",
                column: "catalog_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_items_manipulation_order_id",
                table: "sale_items",
                column: "manipulation_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_items_raw_material_id",
                table: "sale_items",
                column: "raw_material_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_items_sale_id",
                table: "sale_items",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_payments_processed_by_employee_id",
                table: "sale_payments",
                column: "processed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_payments_sale_id",
                table: "sale_payments",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "IX_search_history_created_at",
                table: "search_history",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_search_history_customer_id",
                table: "search_history",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_search_history_search_term",
                table: "search_history",
                column: "search_term");

            migrationBuilder.CreateIndex(
                name: "idx_subscription_invoices_created_at",
                table: "subscription_invoices",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_subscription_invoices_status",
                table: "subscription_invoices",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_subscription_invoices_status_due_date",
                table: "subscription_invoices",
                columns: new[] { "status", "due_date" });

            migrationBuilder.CreateIndex(
                name: "idx_subscription_invoices_stripe_invoice_id",
                table: "subscription_invoices",
                column: "stripe_invoice_id",
                unique: true,
                filter: "stripe_invoice_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_subscription_invoices_subscription_id",
                table: "subscription_invoices",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_establishment_id",
                table: "subscriptions",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_gateway_config_id",
                table: "subscriptions",
                column: "gateway_config_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_subscription_plan_id",
                table: "subscriptions",
                column: "subscription_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_certifications_certification_type",
                table: "supplier_certifications",
                column: "certification_type");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_certifications_establishment_id",
                table: "supplier_certifications",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_certifications_expiration_date",
                table: "supplier_certifications",
                column: "expiration_date");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_certifications_status",
                table: "supplier_certifications",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_certifications_supplier_id",
                table: "supplier_certifications",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_quality_scores_establishment_id",
                table: "supplier_quality_scores",
                column: "establishment_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_quality_scores_supplier_id",
                table: "supplier_quality_scores",
                column: "supplier_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_RawMaterials_RawMaterialId",
                table: "Batches",
                column: "RawMaterialId",
                principalTable: "RawMaterials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_suppliers_SupplierId",
                table: "Batches",
                column: "SupplierId",
                principalTable: "suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BatchReceivings_Batches_BatchId",
                table: "BatchReceivings",
                column: "BatchId",
                principalTable: "Batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BatchReceivings_PurchaseOrderItems_PurchaseOrderItemId",
                table: "BatchReceivings",
                column: "PurchaseOrderItemId",
                principalTable: "PurchaseOrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BatchReceivings_employees_ReceivedByEmployeeId",
                table: "BatchReceivings",
                column: "ReceivedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_client_onboarding_Establishments_establishment_id",
                table: "client_onboarding",
                column: "establishment_id",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Establishments_AccessLevels_AccessLevelId",
                table: "Establishments",
                column: "AccessLevelId",
                principalTable: "AccessLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FormulaComponents_RawMaterials_RawMaterialId",
                table: "FormulaComponents",
                column: "RawMaterialId",
                principalTable: "RawMaterials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Formulas_Establishments_EstablishmentId",
                table: "Formulas",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_Establishments_EstablishmentId",
                table: "ManipulationOrders",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_Formulas_FormulaId",
                table: "ManipulationOrders",
                column: "FormulaId",
                principalTable: "Formulas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_employees_ApprovedByPharmacistId",
                table: "ManipulationOrders",
                column: "ApprovedByPharmacistId",
                principalTable: "employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_employees_CheckedByEmployeeId",
                table: "ManipulationOrders",
                column: "CheckedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_employees_ManipulatedByEmployeeId",
                table: "ManipulationOrders",
                column: "ManipulatedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_employees_RequestedByEmployeeId",
                table: "ManipulationOrders",
                column: "RequestedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_password_reset_tokens_employees_EmployeeId",
                table: "password_reset_tokens",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_RawMaterials_RawMaterialId",
                table: "PurchaseOrderItems",
                column: "RawMaterialId",
                principalTable: "RawMaterials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Establishments_EstablishmentId",
                table: "PurchaseOrders",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_employees_ApprovedByEmployeeId",
                table: "PurchaseOrders",
                column: "ApprovedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_employees_CreatedByEmployeeId",
                table: "PurchaseOrders",
                column: "CreatedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_employees_UpdatedByEmployeeId",
                table: "PurchaseOrders",
                column: "UpdatedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_suppliers_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId",
                principalTable: "suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RawMaterials_Establishments_EstablishmentId",
                table: "RawMaterials",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sales_customers_customer_id",
                table: "sales",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_sales_employees_cancelled_by_employee_id",
                table: "sales",
                column: "cancelled_by_employee_id",
                principalTable: "employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_sales_employees_created_by_employee_id",
                table: "sales",
                column: "created_by_employee_id",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sales_employees_updated_by_employee_id",
                table: "sales",
                column: "updated_by_employee_id",
                principalTable: "employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Batches_BatchId",
                table: "StockMovements",
                column: "BatchId",
                principalTable: "Batches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Establishments_EstablishmentId",
                table: "StockMovements",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_RawMaterials_RawMaterialId",
                table: "StockMovements",
                column: "RawMaterialId",
                principalTable: "RawMaterials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_suppliers_Establishments_EstablishmentId",
                table: "suppliers",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Batches_RawMaterials_RawMaterialId",
                table: "Batches");

            migrationBuilder.DropForeignKey(
                name: "FK_Batches_suppliers_SupplierId",
                table: "Batches");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchReceivings_Batches_BatchId",
                table: "BatchReceivings");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchReceivings_PurchaseOrderItems_PurchaseOrderItemId",
                table: "BatchReceivings");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchReceivings_employees_ReceivedByEmployeeId",
                table: "BatchReceivings");

            migrationBuilder.DropForeignKey(
                name: "FK_client_onboarding_Establishments_establishment_id",
                table: "client_onboarding");

            migrationBuilder.DropForeignKey(
                name: "FK_Establishments_AccessLevels_AccessLevelId",
                table: "Establishments");

            migrationBuilder.DropForeignKey(
                name: "FK_FormulaComponents_RawMaterials_RawMaterialId",
                table: "FormulaComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_Formulas_Establishments_EstablishmentId",
                table: "Formulas");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_Establishments_EstablishmentId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_Formulas_FormulaId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_employees_ApprovedByPharmacistId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_employees_CheckedByEmployeeId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_employees_ManipulatedByEmployeeId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_employees_RequestedByEmployeeId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_password_reset_tokens_employees_EmployeeId",
                table: "password_reset_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_RawMaterials_RawMaterialId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Establishments_EstablishmentId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_employees_ApprovedByEmployeeId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_employees_CreatedByEmployeeId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_employees_UpdatedByEmployeeId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_suppliers_SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_RawMaterials_Establishments_EstablishmentId",
                table: "RawMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_sales_customers_customer_id",
                table: "sales");

            migrationBuilder.DropForeignKey(
                name: "FK_sales_employees_cancelled_by_employee_id",
                table: "sales");

            migrationBuilder.DropForeignKey(
                name: "FK_sales_employees_created_by_employee_id",
                table: "sales");

            migrationBuilder.DropForeignKey(
                name: "FK_sales_employees_updated_by_employee_id",
                table: "sales");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Batches_BatchId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Establishments_EstablishmentId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_RawMaterials_RawMaterialId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_suppliers_Establishments_EstablishmentId",
                table: "suppliers");

            migrationBuilder.DropTable(
                name: "active_ingredients");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "auditor_access_logs");

            migrationBuilder.DropTable(
                name: "capsule_size_reference");

            migrationBuilder.DropTable(
                name: "cart_items");

            migrationBuilder.DropTable(
                name: "cash_movements");

            migrationBuilder.DropTable(
                name: "company_settings");

            migrationBuilder.DropTable(
                name: "controlled_inventory_items");

            migrationBuilder.DropTable(
                name: "controlled_substance_balances");

            migrationBuilder.DropTable(
                name: "controlled_substance_movements");

            migrationBuilder.DropTable(
                name: "CouponUsages");

            migrationBuilder.DropTable(
                name: "customer_addresses");

            migrationBuilder.DropTable(
                name: "customer_devices");

            migrationBuilder.DropTable(
                name: "CustomerCartItems");

            migrationBuilder.DropTable(
                name: "CustomerSessions");

            migrationBuilder.DropTable(
                name: "delivery_estimates");

            migrationBuilder.DropTable(
                name: "dual_verifications");

            migrationBuilder.DropTable(
                name: "establishment_pricing_config");

            migrationBuilder.DropTable(
                name: "establishment_pricing_settings");

            migrationBuilder.DropTable(
                name: "EstablishmentQRCodes");

            migrationBuilder.DropTable(
                name: "fiscal_configs");

            migrationBuilder.DropTable(
                name: "fiscal_invoice_items");

            migrationBuilder.DropTable(
                name: "fiscal_logs");

            migrationBuilder.DropTable(
                name: "fiscal_number_gaps");

            migrationBuilder.DropTable(
                name: "fiscal_queue");

            migrationBuilder.DropTable(
                name: "formula_cart_items");

            migrationBuilder.DropTable(
                name: "label_print_logs");

            migrationBuilder.DropTable(
                name: "manipulation_leftovers");

            migrationBuilder.DropTable(
                name: "manipulation_losses");

            migrationBuilder.DropTable(
                name: "manipulation_order_components");

            migrationBuilder.DropTable(
                name: "ManipulationPhotos");

            migrationBuilder.DropTable(
                name: "OnlineOrderItems");

            migrationBuilder.DropTable(
                name: "payment_methods");

            migrationBuilder.DropTable(
                name: "payment_webhook_logs");

            migrationBuilder.DropTable(
                name: "pharmaceutical_form_compositions");

            migrationBuilder.DropTable(
                name: "PharmaceuticalAnalysisLog");

            migrationBuilder.DropTable(
                name: "pharmacist_approvals");

            migrationBuilder.DropTable(
                name: "pharmacy_payout_accounts");

            migrationBuilder.DropTable(
                name: "pharmacy_ratings");

            migrationBuilder.DropTable(
                name: "platform_commissions");

            migrationBuilder.DropTable(
                name: "platform_transactions");

            migrationBuilder.DropTable(
                name: "prescription_files");

            migrationBuilder.DropTable(
                name: "product_ratings");

            migrationBuilder.DropTable(
                name: "production_records");

            migrationBuilder.DropTable(
                name: "Refunds");

            migrationBuilder.DropTable(
                name: "saas_admin_password_resets");

            migrationBuilder.DropTable(
                name: "saas_admin_sessions");

            migrationBuilder.DropTable(
                name: "sale_items");

            migrationBuilder.DropTable(
                name: "sale_payments");

            migrationBuilder.DropTable(
                name: "search_history");

            migrationBuilder.DropTable(
                name: "special_prescription_controls");

            migrationBuilder.DropTable(
                name: "subscription_invoices");

            migrationBuilder.DropTable(
                name: "supplier_certifications");

            migrationBuilder.DropTable(
                name: "supplier_quality_scores");

            migrationBuilder.DropTable(
                name: "auditor_access_requests");

            migrationBuilder.DropTable(
                name: "cash_registers");

            migrationBuilder.DropTable(
                name: "controlled_inventory_checks");

            migrationBuilder.DropTable(
                name: "Coupons");

            migrationBuilder.DropTable(
                name: "CustomerCarts");

            migrationBuilder.DropTable(
                name: "CustomerAuths");

            migrationBuilder.DropTable(
                name: "fiscal_invoices");

            migrationBuilder.DropTable(
                name: "carts");

            migrationBuilder.DropTable(
                name: "generated_labels");

            migrationBuilder.DropTable(
                name: "ManipulationSteps");

            migrationBuilder.DropTable(
                name: "pharmaceutical_form_subtypes");

            migrationBuilder.DropTable(
                name: "prescriptions");

            migrationBuilder.DropTable(
                name: "OnlineOrders");

            migrationBuilder.DropTable(
                name: "customer_formulas");

            migrationBuilder.DropTable(
                name: "CatalogProducts");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "label_templates");

            migrationBuilder.DropTable(
                name: "pharmaceutical_forms");

            migrationBuilder.DropTable(
                name: "prescription_quotes");

            migrationBuilder.DropTable(
                name: "product_sub_types");

            migrationBuilder.DropTable(
                name: "CatalogCategories");

            migrationBuilder.DropTable(
                name: "payment_gateway_configs");

            migrationBuilder.DropTable(
                name: "subscription_plans");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "product_types");

            migrationBuilder.DropTable(
                name: "saas_admins");

            migrationBuilder.DropIndex(
                name: "IX_TwoFactorAuths_EmployeeId",
                table: "TwoFactorAuths");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sales",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "IX_sales_cancelled_by_employee_id",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "IX_sales_customer_id",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "IX_RawMaterials_Category",
                table: "RawMaterials");

            migrationBuilder.DropIndex(
                name: "IX_RawMaterials_IsVirtual",
                table: "RawMaterials");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_EstablishmentId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_client_onboarding_establishment_id",
                table: "client_onboarding");

            migrationBuilder.DropPrimaryKey(
                name: "PK_password_reset_tokens",
                table: "password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "IX_password_reset_tokens_EmployeeId",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "IsUsed",
                table: "TwoFactorAuths");

            migrationBuilder.DropColumn(
                name: "UsedAt",
                table: "TwoFactorAuths");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "cancelled_by_employee_id",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "change_amount",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "discount_percentage",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "has_multiple_payments",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "observations",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "paid_amount",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "AllowedUsage",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "BasePrice",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "BulkDensity",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "CorrectionFactor",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "DilutionFactor",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "Indications",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "IsVirtual",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "LastKnownPrice",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "LastPriceDate",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "LastPurchasePrice",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "LastPurchasePriceDate",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "LossFactor",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "ParticleSize",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "PhysicalState",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "Popularity",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "PriceSource",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "SpecificMarkup",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "Synonyms",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "TappedDensity",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "PrescriptionQuoteId",
                table: "ManipulationOrders");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "ManipulationOrders");

            migrationBuilder.DropColumn(
                name: "code",
                table: "ManipulationOrders");

            migrationBuilder.DropColumn(
                name: "AcceptingOrders",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "AverageDeliveryMinutes",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "DeliveryRadiusKm",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "FeaturesEnabled",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "InscricaoEstadual",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "IsMarketplaceActive",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "MarketplaceDescription",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "MarketplaceOpeningHours",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "MaxEmployeesLimit",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "MaxOrdersLimit",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "MinOrderAmount",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "StripeConnectAccountId",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "Subscription_status",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "TotalRatings",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "crm",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "crm_state",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "Attempts",
                table: "password_reset_tokens");

            migrationBuilder.RenameTable(
                name: "sales",
                newName: "Sales");

            migrationBuilder.RenameTable(
                name: "password_reset_tokens",
                newName: "PasswordResetTokens");

            migrationBuilder.RenameColumn(
                name: "subtotal",
                table: "Sales",
                newName: "SubTotal");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Sales",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Sales",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "total_amount",
                table: "Sales",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "sale_date",
                table: "Sales",
                newName: "SaleDate");

            migrationBuilder.RenameColumn(
                name: "payment_method",
                table: "Sales",
                newName: "PaymentMethod");

            migrationBuilder.RenameColumn(
                name: "invoice_number",
                table: "Sales",
                newName: "InvoiceNumber");

            migrationBuilder.RenameColumn(
                name: "invoice_key",
                table: "Sales",
                newName: "InvoiceKey");

            migrationBuilder.RenameColumn(
                name: "establishment_id",
                table: "Sales",
                newName: "EstablishmentId");

            migrationBuilder.RenameColumn(
                name: "discount_amount",
                table: "Sales",
                newName: "DiscountAmount");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Sales",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "cancellation_reason",
                table: "Sales",
                newName: "CancellationReason");

            migrationBuilder.RenameColumn(
                name: "updated_by_employee_id",
                table: "Sales",
                newName: "AuthorizedByPharmacistId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Sales",
                newName: "PrescriptionDate");

            migrationBuilder.RenameColumn(
                name: "payment_status",
                table: "Sales",
                newName: "PrescriptionNumber");

            migrationBuilder.RenameColumn(
                name: "payment_date",
                table: "Sales",
                newName: "CanceledAt");

            migrationBuilder.RenameColumn(
                name: "invoice_status",
                table: "Sales",
                newName: "PrescriberRegistration");

            migrationBuilder.RenameColumn(
                name: "created_by_employee_id",
                table: "Sales",
                newName: "SoldByEmployeeId");

            migrationBuilder.RenameColumn(
                name: "code",
                table: "Sales",
                newName: "SaleNumber");

            migrationBuilder.RenameIndex(
                name: "IX_sales_updated_by_employee_id",
                table: "Sales",
                newName: "IX_Sales_AuthorizedByPharmacistId");

            migrationBuilder.RenameIndex(
                name: "IX_sales_created_by_employee_id",
                table: "Sales",
                newName: "IX_Sales_SoldByEmployeeId");

            migrationBuilder.AlterColumn<decimal>(
                name: "SubTotal",
                table: "Sales",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Sales",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Sales",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Sales",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceKey",
                table: "Sales",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(44)",
                oldMaxLength: 44,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountAmount",
                table: "Sales",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "CustomerCpf",
                table: "Sales",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Sales",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Sales",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                table: "Sales",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "Sales",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "Sales",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrescriberName",
                table: "Sales",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "RawMaterials",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Establishments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Street",
                table: "Establishments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "Establishments",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "Establishments",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "Establishments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Neighborhood",
                table: "Establishments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Establishments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Cnpj",
                table: "Establishments",
                type: "character varying(14)",
                maxLength: 14,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(14)",
                oldMaxLength: 14,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Establishments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Street",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "employees",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Salary",
                table: "employees",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "employees",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Neighborhood",
                table: "employees",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "HireDate",
                table: "employees",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DateOfBirth",
                table: "employees",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Cpf",
                table: "employees",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(11)",
                oldMaxLength: 11,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "employees",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "AccessLevels",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AccessLevels",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sales",
                table: "Sales",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PasswordResetTokens",
                table: "PasswordResetTokens",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "SaleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ManipulationOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleItems_ManipulationOrders_ManipulationOrderId",
                        column: x => x.ManipulationOrderId,
                        principalTable: "ManipulationOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleItems_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000101"),
                columns: new[] { "Description", "Scope" },
                values: new object[] { "Permite visualizar o estoque", "Establishment" });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000102"),
                columns: new[] { "Action", "Description", "DisplayName", "ResourceAction" },
                values: new object[] { "update", "Permite atualizar quantidades no estoque", "Atualizar Estoque", "inventory.update" });

            migrationBuilder.UpdateData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000201"),
                columns: new[] { "Description", "DisplayName", "Scope" },
                values: new object[] { "Permite realizar vendas", "Realizar Vendas", "Establishment" });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "AccessProfileId", "Action", "AuditLog", "Category", "CreatedAt", "DependsOn", "Description", "DisplayName", "IsActive", "IsSystemPermission", "RequiresApproval", "RequiresTwoFactor", "Resource", "ResourceAction", "RiskLevel", "Scope", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), null, "create", true, "HR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite criar novos funcionários", "Criar Funcionários", true, true, false, false, "employees", "employees.create", "High", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000002"), null, "read", true, "HR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite visualizar dados de funcionários", "Visualizar Funcionários", true, true, false, false, "employees", "employees.read", "Low", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000003"), null, "update", true, "HR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite editar dados de funcionários", "Editar Funcionários", true, true, false, false, "employees", "employees.update", "Medium", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000004"), null, "delete", true, "HR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite deletar funcionários", "Deletar Funcionários", true, true, true, false, "employees", "employees.delete", "Critical", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000005"), null, "terminate", true, "HR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite demitir funcionários", "Demitir Funcionários", true, true, true, false, "employees", "employees.terminate", "Critical", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TwoFactorAuths_Code_Purpose_ExpiresAt",
                table: "TwoFactorAuths",
                columns: new[] { "Code", "Purpose", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TwoFactorAuths_EmployeeId_ExpiresAt",
                table: "TwoFactorAuths",
                columns: new[] { "EmployeeId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_EstablishmentId",
                table: "Sales",
                column: "EstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SaleDate",
                table: "Sales",
                column: "SaleDate");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SaleNumber_EstablishmentId",
                table: "Sales",
                columns: new[] { "SaleNumber", "EstablishmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Status",
                table: "Sales",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_EstablishmentId_OrderDate",
                table: "PurchaseOrders",
                columns: new[] { "EstablishmentId", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_EstablishmentId_Status",
                table: "PurchaseOrders",
                columns: new[] { "EstablishmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_AttemptedAt",
                table: "LoginAttempts",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Identifier",
                table: "LoginAttempts",
                column: "Identifier");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_IpAddress_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "IpAddress", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_Code_ExpiresAt",
                table: "PasswordResetTokens",
                columns: new[] { "Code", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_EmployeeId_ExpiresAt",
                table: "PasswordResetTokens",
                columns: new[] { "EmployeeId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_Token",
                table: "PasswordResetTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_ManipulationOrderId",
                table: "SaleItems",
                column: "ManipulationOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_SaleId",
                table: "SaleItems",
                column: "SaleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_RawMaterials_RawMaterialId",
                table: "Batches",
                column: "RawMaterialId",
                principalTable: "RawMaterials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_suppliers_SupplierId",
                table: "Batches",
                column: "SupplierId",
                principalTable: "suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BatchReceivings_Batches_BatchId",
                table: "BatchReceivings",
                column: "BatchId",
                principalTable: "Batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BatchReceivings_PurchaseOrderItems_PurchaseOrderItemId",
                table: "BatchReceivings",
                column: "PurchaseOrderItemId",
                principalTable: "PurchaseOrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BatchReceivings_employees_ReceivedByEmployeeId",
                table: "BatchReceivings",
                column: "ReceivedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Establishments_AccessLevels_AccessLevelId",
                table: "Establishments",
                column: "AccessLevelId",
                principalTable: "AccessLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FormulaComponents_RawMaterials_RawMaterialId",
                table: "FormulaComponents",
                column: "RawMaterialId",
                principalTable: "RawMaterials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Formulas_Establishments_EstablishmentId",
                table: "Formulas",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_Establishments_EstablishmentId",
                table: "ManipulationOrders",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_Formulas_FormulaId",
                table: "ManipulationOrders",
                column: "FormulaId",
                principalTable: "Formulas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_employees_ApprovedByPharmacistId",
                table: "ManipulationOrders",
                column: "ApprovedByPharmacistId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_employees_CheckedByEmployeeId",
                table: "ManipulationOrders",
                column: "CheckedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_employees_ManipulatedByEmployeeId",
                table: "ManipulationOrders",
                column: "ManipulatedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_employees_RequestedByEmployeeId",
                table: "ManipulationOrders",
                column: "RequestedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordResetTokens_employees_EmployeeId",
                table: "PasswordResetTokens",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_RawMaterials_RawMaterialId",
                table: "PurchaseOrderItems",
                column: "RawMaterialId",
                principalTable: "RawMaterials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Establishments_EstablishmentId",
                table: "PurchaseOrders",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_employees_ApprovedByEmployeeId",
                table: "PurchaseOrders",
                column: "ApprovedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_employees_CreatedByEmployeeId",
                table: "PurchaseOrders",
                column: "CreatedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_employees_UpdatedByEmployeeId",
                table: "PurchaseOrders",
                column: "UpdatedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_suppliers_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId",
                principalTable: "suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RawMaterials_Establishments_EstablishmentId",
                table: "RawMaterials",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Establishments_EstablishmentId",
                table: "Sales",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_employees_AuthorizedByPharmacistId",
                table: "Sales",
                column: "AuthorizedByPharmacistId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_employees_SoldByEmployeeId",
                table: "Sales",
                column: "SoldByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Batches_BatchId",
                table: "StockMovements",
                column: "BatchId",
                principalTable: "Batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Establishments_EstablishmentId",
                table: "StockMovements",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_RawMaterials_RawMaterialId",
                table: "StockMovements",
                column: "RawMaterialId",
                principalTable: "RawMaterials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_suppliers_Establishments_EstablishmentId",
                table: "suppliers",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
