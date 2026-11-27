using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryX.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PaymentAttemptCheckoutState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalMessage",
                table: "PaymentAttempts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorizationUrl",
                table: "PaymentAttempts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalMessage",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "AuthorizationUrl",
                table: "PaymentAttempts");
        }
    }
}
