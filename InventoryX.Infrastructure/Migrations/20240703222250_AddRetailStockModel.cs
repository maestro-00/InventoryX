using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetailStockModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RetailStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(17,3)", nullable: false),
                    Created_At = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Updated_At = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetailStock_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetailStock_InventoryItemId",
                table: "RetailStock",
                column: "InventoryItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetailStock");
        }
    }
}
