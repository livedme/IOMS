using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update_contact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Suppliers",
                newName: "SupplierPhone");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Suppliers",
                newName: "SupplierName");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Suppliers",
                newName: "SupplierEmail");

            migrationBuilder.RenameIndex(
                name: "IX_Suppliers_TenantId_Email",
                table: "Suppliers",
                newName: "IX_Suppliers_TenantId_SupplierEmail");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Customers",
                newName: "CustomerPhone");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Customers",
                newName: "CustomerName");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Customers",
                newName: "CustomerEmail");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_TenantId_Email",
                table: "Customers",
                newName: "IX_Customers_TenantId_CustomerEmail");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_Name",
                table: "Customers",
                newName: "IX_Customers_CustomerName");

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonEmail",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonName",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonPhone",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonEmail",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonName",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonPhone",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactPersonEmail",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ContactPersonName",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ContactPersonPhone",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ContactPersonEmail",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ContactPersonName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ContactPersonPhone",
                table: "Customers");

            migrationBuilder.RenameColumn(
                name: "SupplierPhone",
                table: "Suppliers",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "SupplierName",
                table: "Suppliers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "SupplierEmail",
                table: "Suppliers",
                newName: "Email");

            migrationBuilder.RenameIndex(
                name: "IX_Suppliers_TenantId_SupplierEmail",
                table: "Suppliers",
                newName: "IX_Suppliers_TenantId_Email");

            migrationBuilder.RenameColumn(
                name: "CustomerPhone",
                table: "Customers",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "CustomerName",
                table: "Customers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "CustomerEmail",
                table: "Customers",
                newName: "Email");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_TenantId_CustomerEmail",
                table: "Customers",
                newName: "IX_Customers_TenantId_Email");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_CustomerName",
                table: "Customers",
                newName: "IX_Customers_Name");
        }
    }
}
