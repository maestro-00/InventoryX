using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryX.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BillingQuoteSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillingQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetPlanDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetCycle = table.Column<int>(type: "integer", nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    PriceVersion = table.Column<string>(type: "text", nullable: false),
                    BaseAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    EffectiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextRenewalAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextRenewalTotal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingQuotes_Subscriptions_TenantId_SubscriptionId",
                        columns: x => new { x.TenantId, x.SubscriptionId },
                        principalTable: "Subscriptions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingQuotes_TenantId_ExpiresAt",
                table: "BillingQuotes",
                columns: new[] { "TenantId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingQuotes_TenantId_SubscriptionId",
                table: "BillingQuotes",
                columns: new[] { "TenantId", "SubscriptionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingQuotes");
        }
    }
}
