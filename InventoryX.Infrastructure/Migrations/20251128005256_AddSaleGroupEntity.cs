using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleGroupEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_SaleGroup_SaleGroupId",
                table: "Sales");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SaleGroup",
                table: "SaleGroup");

            migrationBuilder.RenameTable(
                name: "SaleGroup",
                newName: "SaleGroups");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SaleGroups",
                table: "SaleGroups",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_SaleGroups_SaleGroupId",
                table: "Sales",
                column: "SaleGroupId",
                principalTable: "SaleGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_SaleGroups_SaleGroupId",
                table: "Sales");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SaleGroups",
                table: "SaleGroups");

            migrationBuilder.RenameTable(
                name: "SaleGroups",
                newName: "SaleGroup");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SaleGroup",
                table: "SaleGroup",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_SaleGroup_SaleGroupId",
                table: "Sales",
                column: "SaleGroupId",
                principalTable: "SaleGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
