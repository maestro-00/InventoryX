using System.Security.Cryptography;
using System.Text;
using InventoryX.Application.Services.IServices;
using Microsoft.Extensions.Configuration;

namespace InventoryX.Infrastructure.Services;

/// <summary>AES-256-GCM envelope for reusable provider authorizations and protected phone numbers.</summary>
public sealed class BillingSecretProtector(IConfiguration configuration) : IBillingSecretProtector
{
    private const string Prefix = "v1:";

    public string Protect(Guid tenantId, string secret)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("A tenant is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(secret)) throw new ArgumentException("A secret is required.", nameof(secret));
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(secret);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(GetKey(), 16))
            aes.Encrypt(nonce, plaintext, ciphertext, tag, tenantId.ToByteArray());
        return Prefix + Convert.ToBase64String([.. nonce, .. tag, .. ciphertext]);
    }

    public string Unprotect(Guid tenantId, string ciphertext)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("A tenant is required.", nameof(tenantId));
        if (!ciphertext.StartsWith(Prefix, StringComparison.Ordinal))
            throw new CryptographicException("Unsupported billing secret envelope.");
        var payload = Convert.FromBase64String(ciphertext[Prefix.Length..]);
        if (payload.Length < 28) throw new CryptographicException("Invalid billing secret envelope.");
        var plaintext = new byte[payload.Length - 28];
        using (var aes = new AesGcm(GetKey(), 16))
            aes.Decrypt(payload.AsSpan(0, 12), payload.AsSpan(28), payload.AsSpan(12, 16), plaintext, tenantId.ToByteArray());
        return Encoding.UTF8.GetString(plaintext);
    }

    private byte[] GetKey()
    {
        var setting = configuration["Billing:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(setting))
            throw new InvalidOperationException("Billing:EncryptionKey is required to store billing secrets.");
        var key = Convert.FromBase64String(setting);
        if (key.Length != 32)
            throw new InvalidOperationException("Billing:EncryptionKey must contain 32 random bytes encoded as base64.");
        return key;
    }
}
