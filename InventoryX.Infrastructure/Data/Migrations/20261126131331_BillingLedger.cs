using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryX.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BillingLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BillingAnchor",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentPaymentMethodId",
                table: "Subscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetentionWarningAcceptedAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingContactSnapshot",
                table: "BillingInvoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BillingPeriodId",
                table: "BillingInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueAt",
                table: "BillingInvoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAt",
                table: "BillingInvoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentIntentId",
                table: "BillingInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodEnd",
                table: "BillingInvoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodStart",
                table: "BillingInvoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxDetailsSnapshot",
                table: "BillingInvoices",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillingPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    PlanDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceVersion = table.Column<string>(type: "text", nullable: false),
                    BaseAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WaivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WaiverReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentIntentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentMethodId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InitiatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SafeFailureCode = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentIntents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingPeriodId = table.Column<Guid>(type: "uuid", nullable: true),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    TargetPlanDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetCycle = table.Column<int>(type: "integer", nullable: true),
                    BaseAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    QuoteExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpectedPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: false),
                    RequestHash = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentIntents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    RenewalMode = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    MaskedIdentifier = table.Column<string>(type: "text", nullable: false),
                    Brand = table.Column<string>(type: "text", nullable: true),
                    ExpiryMonth = table.Column<int>(type: "integer", nullable: true),
                    ExpiryYear = table.Column<int>(type: "integer", nullable: true),
                    EncryptedAuthorizationCode = table.Column<string>(type: "text", nullable: true),
                    EncryptedPhoneNumber = table.Column<string>(type: "text", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaystackEventInbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderEventId = table.Column<string>(type: "text", nullable: true),
                    PayloadHash = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    SignatureMode = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SafeErrorSummary = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaystackEventInbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledSubscriptionChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    TargetPlanDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetCycle = table.Column<int>(type: "integer", nullable: true),
                    QuotedNextPeriodAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    EffectiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExceededLimitsJson = table.Column<string>(type: "text", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledSubscriptionChanges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId",
                table: "Subscriptions",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_PaymentIntentId",
                table: "BillingInvoices",
                column: "PaymentIntentId",
                unique: true,
                filter: "\"PaymentIntentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPeriods_SubscriptionId_StartAt_Cycle",
                table: "BillingPeriods",
                columns: new[] { "SubscriptionId", "StartAt", "Cycle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingPeriods_TenantId_Status_EndAt",
                table: "BillingPeriods",
                columns: new[] { "TenantId", "Status", "EndAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_PaymentIntentId_Status",
                table: "PaymentAttempts",
                columns: new[] { "PaymentIntentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_Reference",
                table: "PaymentAttempts",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_BillingPeriodId_Purpose",
                table: "PaymentIntents",
                columns: new[] { "BillingPeriodId", "Purpose" },
                unique: true,
                filter: "\"BillingPeriodId\" IS NOT NULL AND \"Purpose\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_TenantId_IdempotencyKey",
                table: "PaymentIntents",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_TenantId_Status",
                table: "PaymentMethods",
                columns: new[] { "TenantId", "Status" },
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PaystackEventInbox_PayloadHash_Reference_EventType",
                table: "PaystackEventInbox",
                columns: new[] { "PayloadHash", "Reference", "EventType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaystackEventInbox_ProviderEventId",
                table: "PaystackEventInbox",
                column: "ProviderEventId",
                unique: true,
                filter: "\"ProviderEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSubscriptionChanges_SubscriptionId_Status",
                table: "ScheduledSubscriptionChanges",
                columns: new[] { "SubscriptionId", "Status" },
                unique: true,
                filter: "\"Status\" = 0");

            // Existing authorization references are deliberately not copied to PaymentMethods:
            // Paystack must verify channel, tenant and card reusability before activation.
            migrationBuilder.Sql("""
                UPDATE "Subscriptions"
                SET "BillingAnchor" = CASE
                    WHEN "CurrentPeriodEnd" > "CurrentPeriodStart" THEN "CurrentPeriodStart"
                    ELSE "CreatedAt"
                END;

                INSERT INTO "BillingPeriods"
                    ("Id", "TenantId", "SubscriptionId", "StartAt", "EndAt", "Cycle",
                     "PlanDefinitionId", "PriceVersion", "BaseAmount", "TaxAmount",
                     "TotalAmount", "Currency", "Status", "WaivedAt", "WaiverReason", "CreatedAt")
                SELECT gen_random_uuid(), s."TenantId", s."Id", s."CurrentPeriodStart",
                       s."CurrentPeriodEnd", s."BillingCycle", s."PlanDefinitionId",
                       'legacy-unverified', 0, 0, 0, COALESCE(t."Currency", 'GHS'),
                       3, CURRENT_TIMESTAMP, 'legacy-migration-no-charge', CURRENT_TIMESTAMP
                FROM "Subscriptions" AS s
                LEFT JOIN "Tenants" AS t ON t."Id" = s."TenantId"
                WHERE s."CurrentPeriodEnd" > s."CurrentPeriodStart"
                  AND NOT EXISTS (
                      SELECT 1 FROM "BillingPeriods" AS p
                      WHERE p."SubscriptionId" = s."Id"
                        AND p."StartAt" = s."CurrentPeriodStart"
                        AND p."Cycle" = s."BillingCycle");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingPeriods");

            migrationBuilder.DropTable(
                name: "PaymentAttempts");

            migrationBuilder.DropTable(
                name: "PaymentIntents");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropTable(
                name: "PaystackEventInbox");

            migrationBuilder.DropTable(
                name: "ScheduledSubscriptionChanges");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TenantId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_BillingInvoices_PaymentIntentId",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "BillingAnchor",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CurrentPaymentMethodId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RetentionWarningAcceptedAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "BillingContactSnapshot",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "BillingPeriodId",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "IssuedAt",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "PaymentIntentId",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "PeriodEnd",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "PeriodStart",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "TaxDetailsSnapshot",
                table: "BillingInvoices");
        }
    }
}
