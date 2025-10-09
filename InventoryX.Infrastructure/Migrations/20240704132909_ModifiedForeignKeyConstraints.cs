using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedForeignKeyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_InventoryItems_InventoryItemId",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_InventoryItems_InventoryItemId",
                table: "Sales");

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_InventoryItems_InventoryItemId",
                table: "Purchases",
                column: "InventoryItemId",
                principalTable: "InventoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_InventoryItems_InventoryItemId",
                table: "Sales",
                column: "InventoryItemId",
                principalTable: "InventoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_InventoryItems_InventoryItemId",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_InventoryItems_InventoryItemId",
                table: "Sales");

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_InventoryItems_InventoryItemId",
                table: "Purchases",
                column: "InventoryItemId",
                principalTable: "InventoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_InventoryItems_InventoryItemId",
                table: "Sales",
                column: "InventoryItemId",
                principalTable: "InventoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
