using System.Security.Cryptography;
using FluentAssertions;
using InventoryX.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace InventoryX.Infrastructure.Tests.Services;

public sealed class BillingSecretProtectorTests
{
    [Fact]
    public void Authorization_is_encrypted_and_bound_to_its_tenant()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Billing:EncryptionKey"] = key,
        }).Build();
        var protector = new BillingSecretProtector(configuration);
        var tenant = Guid.NewGuid();
        var ciphertext = protector.Protect(tenant, "AUTH_reusable_test");
        ciphertext.Should().StartWith("v1:").And.NotContain("AUTH_reusable_test");
        protector.Unprotect(tenant, ciphertext).Should().Be("AUTH_reusable_test");
        FluentActions.Invoking(() => protector.Unprotect(Guid.NewGuid(), ciphertext))
            .Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Missing_key_never_falls_back_to_plaintext()
    {
        var protector = new BillingSecretProtector(new ConfigurationBuilder().Build());
        FluentActions.Invoking(() => protector.Protect(Guid.NewGuid(), "AUTH_test"))
            .Should().Throw<InvalidOperationException>();
    }
}
