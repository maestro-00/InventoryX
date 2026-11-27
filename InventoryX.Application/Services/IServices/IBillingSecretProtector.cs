namespace InventoryX.Application.Services.IServices;

/// <summary>Encrypts provider authorizations with tenant-bound authenticated encryption.</summary>
public interface IBillingSecretProtector
{
    string Protect(Guid tenantId, string secret);
    string Unprotect(Guid tenantId, string ciphertext);
}
