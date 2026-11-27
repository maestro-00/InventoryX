using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using InventoryX.Application.Options;
using InventoryX.Domain.Models.Tenancy;
using InventoryX.Infrastructure.Data;
using InventoryX.Presentation.Tests.Scenarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace InventoryX.Presentation.Tests.Billing;

public sealed class BillingRenewalContractTests
{
    [Fact]
    public async Task V2_openapi_exposes_owner_method_contact_invoice_and_pdf_routes()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v2/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        paths.GetProperty("/api/v2/billing/payment-methods").GetProperty("get")
            .GetProperty("responses").TryGetProperty("200", out _).Should().BeTrue();
        paths.GetProperty("/api/v2/billing/payment-methods").GetProperty("post")
            .GetProperty("responses").TryGetProperty("202", out _).Should().BeTrue();
        paths.GetProperty("/api/v2/billing/contact").GetProperty("patch")
            .GetProperty("responses").TryGetProperty("204", out _).Should().BeTrue();
        paths.GetProperty("/api/v2/billing/invoices/{id}/pdf").GetProperty("get")
            .GetProperty("responses").TryGetProperty("200", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Invoice_list_is_paged_and_pdf_is_tenant_scoped()
    {
        await using var factory = new TestAppFactory();
        await factory.InitializeDatabaseAsync();
        var ownTenant = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();
        Guid ownInvoice;
        Guid otherInvoice;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var planId = await context.PlanDefinitions.Select(plan => plan.Id).FirstAsync();
            var ownSubscription = new Subscription { TenantId = ownTenant, PlanDefinitionId = planId };
            var otherSubscription = new Subscription { TenantId = otherTenant, PlanDefinitionId = planId };
            context.Tenants.AddRange(new Tenant { Id = ownTenant, Name = "Own" },
                new Tenant { Id = otherTenant, Name = "Other" });
            context.Subscriptions.AddRange(ownSubscription, otherSubscription);
            context.PaymentMethods.AddRange(
                new PaymentMethod { TenantId = ownTenant, Kind = PaymentMethodKind.MobileMoney,
                    RenewalMode = PaymentRenewalMode.OwnerApproval, Provider = "mtn",
                    MaskedIdentifier = "***4567", EncryptedPhoneNumber = "v1:secret-own" },
                new PaymentMethod { TenantId = otherTenant, Kind = PaymentMethodKind.Card,
                    RenewalMode = PaymentRenewalMode.Automatic, Provider = "paystack",
                    MaskedIdentifier = "**** 9999", EncryptedAuthorizationCode = "v1:secret-other" });
            var invoices = Enumerable.Range(1, 3).Select(index => new BillingInvoice
            {
                TenantId = ownTenant, SubscriptionId = ownSubscription.Id, Number = $"OWN-{index}",
                PdfContent = Convert.ToBase64String(Encoding.ASCII.GetBytes("%PDF-1.4\n%%EOF")),
            }).ToArray();
            var foreign = new BillingInvoice { TenantId = otherTenant,
                SubscriptionId = otherSubscription.Id, Number = "OTHER-1",
                PdfContent = invoices[0].PdfContent };
            context.BillingInvoices.AddRange(invoices);
            context.BillingInvoices.Add(foreign);
            await context.SaveChangesAsync();
            ownInvoice = invoices[0].Id;
            otherInvoice = foreign.Id;
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Token(ownTenant, "Owner");
        var list = await client.GetAsync("/api/v2/billing/invoices?page=1&pageSize=2");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("items").GetArrayLength().Should().Be(2);
        body.RootElement.GetProperty("totalCount").GetInt32().Should().Be(3);
        var methods = await client.GetAsync("/api/v2/billing/payment-methods");
        methods.StatusCode.Should().Be(HttpStatusCode.OK);
        var methodBody = await methods.Content.ReadAsStringAsync();
        methodBody.Should().Contain("***4567").And.NotContain("**** 9999")
            .And.NotContain("secret-own").And.NotContain("secret-other");
        var ownPdf = await client.GetAsync($"/api/v2/billing/invoices/{ownInvoice}/pdf");
        ownPdf.StatusCode.Should().Be(HttpStatusCode.OK);
        ownPdf.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        (await ownPdf.Content.ReadAsByteArrayAsync()).Should().StartWith(Encoding.ASCII.GetBytes("%PDF"));
        (await client.GetAsync($"/api/v2/billing/invoices/{otherInvoice}/pdf"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        client.DefaultRequestHeaders.Authorization = Token(ownTenant, "Staff");
        (await client.GetAsync("/api/v2/billing/payment-methods")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync($"/api/v2/billing/invoices/{ownInvoice}/pdf"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static AuthenticationHeaderValue Token(Guid tenantId, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtOptions.DevelopmentSigningKey));
        var token = new JwtSecurityToken(
            issuer: "InventoryX", audience: "InventoryX.Api",
            claims: [new Claim("tenant_id", tenantId.ToString()), new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }
}
