using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FreightAmount",
                table: "SalesOrders",
                newName: "TruckCharge");

            migrationBuilder.AddColumn<string>(
                name: "Chalan",
                table: "SalesOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "SalesOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DueAmount",
                table: "SalesOrders",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LabourCharge",
                table: "SalesOrders",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Naration",
                table: "SalesOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "SalesOrders",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Chalan",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "DueAmount",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "LabourCharge",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "Naration",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "SalesOrders");

            migrationBuilder.RenameColumn(
                name: "TruckCharge",
                table: "SalesOrders",
                newName: "FreightAmount");
        }
    }
}
