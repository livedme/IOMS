using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatetables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LineTotal",
                table: "SalesOrderItems");

            migrationBuilder.RenameColumn(
                name: "SubTotal",
                table: "SalesOrders",
                newName: "ItemsTotalPrice");

            migrationBuilder.RenameColumn(
                name: "TaxRate",
                table: "SalesOrderItems",
                newName: "TotalDiscount");

            migrationBuilder.RenameColumn(
                name: "TaxAmount",
                table: "SalesOrderItems",
                newName: "LineTotalPrice");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ItemsTotalPrice",
                table: "SalesOrders",
                newName: "SubTotal");

            migrationBuilder.RenameColumn(
                name: "TotalDiscount",
                table: "SalesOrderItems",
                newName: "TaxRate");

            migrationBuilder.RenameColumn(
                name: "LineTotalPrice",
                table: "SalesOrderItems",
                newName: "TaxAmount");

            migrationBuilder.AddColumn<decimal>(
                name: "LineTotal",
                table: "SalesOrderItems",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
