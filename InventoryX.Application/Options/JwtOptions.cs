namespace InventoryX.Application.Options
{
    /// <summary>JWT issuance/validation settings; the signing key comes from environment configuration.</summary>
    public class JwtOptions
    {
        public const string SectionName = "Jwt";
        public const string DevelopmentSigningKey =
            "inventoryx-development-signing-key-do-not-use-in-production";

        public string Issuer { get; set; } = "InventoryX";
        public string Audience { get; set; } = "InventoryX.Api";
        public string SigningKey { get; set; } = string.Empty;
        public int AccessTokenMinutes { get; set; } = 30;
        public int RefreshTokenDays { get; set; } = 14;
        /// <summary>Lifetime of register-scoped PIN-exchange tokens (research R3).</summary>
        public int RegisterTokenMinutes { get; set; } = 720;

        /// <summary>
        /// Empty or template placeholder keys fall back to the development key so
        /// token issuance and bearer validation never diverge.
        /// </summary>
        public string ResolveSigningKey() =>
            string.IsNullOrWhiteSpace(SigningKey) ||
            SigningKey.Contains("Your Signing Key", StringComparison.OrdinalIgnoreCase)
                ? DevelopmentSigningKey
                : SigningKey;
    }
}
