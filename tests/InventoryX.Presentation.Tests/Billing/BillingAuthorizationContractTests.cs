using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

public sealed class BillingAuthorizationContractTests
{
    [Fact]
    public async Task Private_v2_billing_requires_an_owner()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient();
        (await client.GetAsync("/api/v2/billing/subscription")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        client.DefaultRequestHeaders.Authorization = Token(Guid.NewGuid(), "Staff");
        (await client.GetAsync("/api/v2/billing/subscription")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Owner_invoice_history_does_not_expose_another_business()
    {
        await using var factory = new TestAppFactory();
        await factory.InitializeDatabaseAsync();
        var ownTenant = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var planId = await context.PlanDefinitions.Select(plan => plan.Id).FirstAsync();
            foreach (var (tenantId, number) in new[] { (ownTenant, "OWN-1"), (otherTenant, "OTHER-1") })
            {
                var subscription = new Subscription { TenantId = tenantId, PlanDefinitionId = planId };
                context.Tenants.Add(new Tenant { Id = tenantId, Name = number });
                context.Subscriptions.Add(subscription);
                context.BillingInvoices.Add(new BillingInvoice
                {
                    TenantId = tenantId, SubscriptionId = subscription.Id, Number = number,
                });
            }
            await context.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Token(ownTenant, "Owner");
        var response = await client.GetAsync("/api/v2/billing/invoices?page=1&pageSize=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("OWN-1").And.NotContain("OTHER-1");
    }

    [Fact]
    public async Task Reusing_a_mutation_key_with_a_different_body_returns_conflict_problem()
    {
        await using var factory = new TestAppFactory();
        await factory.InitializeDatabaseAsync();
        var tenantId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var planId = await context.PlanDefinitions.Select(plan => plan.Id).FirstAsync();
            context.Tenants.Add(new Tenant { Id = tenantId, Name = "Idempotency Test" });
            context.Subscriptions.Add(new Subscription { TenantId = tenantId, PlanDefinitionId = planId });
            await context.SaveChangesAsync();
        }
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Token(tenantId, "Owner");
        client.DefaultRequestHeaders.Add("Idempotency-Key", "billing-contact-1");
        var first = await client.PatchAsJsonAsync("/api/v2/billing/contact", new { billingEmail = "first@example.com", taxNumber = (string?)null });
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var replay = await client.PatchAsJsonAsync("/api/v2/billing/contact", new { billingEmail = "first@example.com", taxNumber = (string?)null });
        replay.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var changed = await client.PatchAsJsonAsync("/api/v2/billing/contact", new { billingEmail = "second@example.com", taxNumber = (string?)null });
        changed.StatusCode.Should().Be(HttpStatusCode.Conflict);
        changed.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var problem = JsonDocument.Parse(await changed.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("billing_idempotency_conflict");
        using var scopeAfter = factory.Services.CreateScope();
        var contextAfter = scopeAfter.ServiceProvider.GetRequiredService<AppDbContext>();
        (await contextAfter.Tenants.SingleAsync(tenant => tenant.Id == tenantId)).BillingEmail
            .Should().Be("first@example.com");
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
