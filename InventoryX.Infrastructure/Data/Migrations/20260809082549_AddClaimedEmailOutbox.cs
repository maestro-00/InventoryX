using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryX.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimedEmailOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAt_OccurredAt",
                table: "OutboxMessages");

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableAt",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimExpiresAt",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimedBy",
                table: "OutboxMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "OutboxMessages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt_AvailableAt_ClaimExpiresAt",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "AvailableAt", "ClaimExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_TenantId_IdempotencyKey",
                table: "OutboxMessages",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAt_AvailableAt_ClaimExpiresAt",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_TenantId_IdempotencyKey",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "AvailableAt",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ClaimExpiresAt",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ClaimedBy",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt_OccurredAt",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "OccurredAt" });
        }
    }
}
