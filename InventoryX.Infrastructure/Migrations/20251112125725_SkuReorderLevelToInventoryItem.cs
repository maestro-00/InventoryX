using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SkuReorderLevelToInventoryItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReOrderLevel",
                table: "InventoryItems",
                type: "decimal(17,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SKU",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReOrderLevel",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "SKU",
                table: "InventoryItems");
        }
    }
}
