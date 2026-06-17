using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderFields1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "SalesOrderItems");

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "SalesQuotes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "SalesQuoteItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "SalesOrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "SalesQuotes");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "SalesQuoteItems");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "SalesOrderItems");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "SalesOrderItems",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
