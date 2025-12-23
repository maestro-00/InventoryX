using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateSaleGroupTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SaleGroupId",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SaleGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Created_At = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Updated_At = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleGroup", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SaleGroupId",
                table: "Sales",
                column: "SaleGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_SaleGroup_SaleGroupId",
                table: "Sales",
                column: "SaleGroupId",
                principalTable: "SaleGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_SaleGroup_SaleGroupId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "SaleGroup");

            migrationBuilder.DropIndex(
                name: "IX_Sales_SaleGroupId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "SaleGroupId",
                table: "Sales");
        }
    }
}
