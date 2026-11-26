using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryX.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BillingLedgerTenantKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Subscriptions_TenantId_Id",
                table: "Subscriptions",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PaymentMethods_TenantId_Id",
                table: "PaymentMethods",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PaymentIntents_TenantId_Id",
                table: "PaymentIntents",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_BillingPeriods_TenantId_Id",
                table: "BillingPeriods",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_CurrentPaymentMethodId",
                table: "Subscriptions",
                columns: new[] { "TenantId", "CurrentPaymentMethodId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSubscriptionChanges_TenantId_SubscriptionId",
                table: "ScheduledSubscriptionChanges",
                columns: new[] { "TenantId", "SubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_TenantId_BillingPeriodId",
                table: "PaymentIntents",
                columns: new[] { "TenantId", "BillingPeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_TenantId_SubscriptionId",
                table: "PaymentIntents",
                columns: new[] { "TenantId", "SubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_TenantId_PaymentIntentId",
                table: "PaymentAttempts",
                columns: new[] { "TenantId", "PaymentIntentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_TenantId_PaymentMethodId",
                table: "PaymentAttempts",
                columns: new[] { "TenantId", "PaymentMethodId" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPeriods_TenantId_SubscriptionId",
                table: "BillingPeriods",
                columns: new[] { "TenantId", "SubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_TenantId_BillingPeriodId",
                table: "BillingInvoices",
                columns: new[] { "TenantId", "BillingPeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_TenantId_PaymentIntentId",
                table: "BillingInvoices",
                columns: new[] { "TenantId", "PaymentIntentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_BillingInvoices_BillingPeriods_TenantId_BillingPeriodId",
                table: "BillingInvoices",
                columns: new[] { "TenantId", "BillingPeriodId" },
                principalTable: "BillingPeriods",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BillingInvoices_PaymentIntents_TenantId_PaymentIntentId",
                table: "BillingInvoices",
                columns: new[] { "TenantId", "PaymentIntentId" },
                principalTable: "PaymentIntents",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BillingPeriods_Subscriptions_TenantId_SubscriptionId",
                table: "BillingPeriods",
                columns: new[] { "TenantId", "SubscriptionId" },
                principalTable: "Subscriptions",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAttempts_PaymentIntents_TenantId_PaymentIntentId",
                table: "PaymentAttempts",
                columns: new[] { "TenantId", "PaymentIntentId" },
                principalTable: "PaymentIntents",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAttempts_PaymentMethods_TenantId_PaymentMethodId",
                table: "PaymentAttempts",
                columns: new[] { "TenantId", "PaymentMethodId" },
                principalTable: "PaymentMethods",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentIntents_BillingPeriods_TenantId_BillingPeriodId",
                table: "PaymentIntents",
                columns: new[] { "TenantId", "BillingPeriodId" },
                principalTable: "BillingPeriods",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentIntents_Subscriptions_TenantId_SubscriptionId",
                table: "PaymentIntents",
                columns: new[] { "TenantId", "SubscriptionId" },
                principalTable: "Subscriptions",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledSubscriptionChanges_Subscriptions_TenantId_Subscri~",
                table: "ScheduledSubscriptionChanges",
                columns: new[] { "TenantId", "SubscriptionId" },
                principalTable: "Subscriptions",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_PaymentMethods_TenantId_CurrentPaymentMethodId",
                table: "Subscriptions",
                columns: new[] { "TenantId", "CurrentPaymentMethodId" },
                principalTable: "PaymentMethods",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingInvoices_BillingPeriods_TenantId_BillingPeriodId",
                table: "BillingInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_BillingInvoices_PaymentIntents_TenantId_PaymentIntentId",
                table: "BillingInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_BillingPeriods_Subscriptions_TenantId_SubscriptionId",
                table: "BillingPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAttempts_PaymentIntents_TenantId_PaymentIntentId",
                table: "PaymentAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAttempts_PaymentMethods_TenantId_PaymentMethodId",
                table: "PaymentAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentIntents_BillingPeriods_TenantId_BillingPeriodId",
                table: "PaymentIntents");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentIntents_Subscriptions_TenantId_SubscriptionId",
                table: "PaymentIntents");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledSubscriptionChanges_Subscriptions_TenantId_Subscri~",
                table: "ScheduledSubscriptionChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_PaymentMethods_TenantId_CurrentPaymentMethodId",
                table: "Subscriptions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Subscriptions_TenantId_Id",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TenantId_CurrentPaymentMethodId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledSubscriptionChanges_TenantId_SubscriptionId",
                table: "ScheduledSubscriptionChanges");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PaymentMethods_TenantId_Id",
                table: "PaymentMethods");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PaymentIntents_TenantId_Id",
                table: "PaymentIntents");

            migrationBuilder.DropIndex(
                name: "IX_PaymentIntents_TenantId_BillingPeriodId",
                table: "PaymentIntents");

            migrationBuilder.DropIndex(
                name: "IX_PaymentIntents_TenantId_SubscriptionId",
                table: "PaymentIntents");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAttempts_TenantId_PaymentIntentId",
                table: "PaymentAttempts");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAttempts_TenantId_PaymentMethodId",
                table: "PaymentAttempts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_BillingPeriods_TenantId_Id",
                table: "BillingPeriods");

            migrationBuilder.DropIndex(
                name: "IX_BillingPeriods_TenantId_SubscriptionId",
                table: "BillingPeriods");

            migrationBuilder.DropIndex(
                name: "IX_BillingInvoices_TenantId_BillingPeriodId",
                table: "BillingInvoices");

            migrationBuilder.DropIndex(
                name: "IX_BillingInvoices_TenantId_PaymentIntentId",
                table: "BillingInvoices");
        }
    }
}
