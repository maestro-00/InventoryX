namespace InventoryX.Application.Options
{
    /// <summary>JWT issuance/validation settings; the signing key comes from environment configuration.</summary>
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = "InventoryX";
        public string Audience { get; set; } = "InventoryX.Api";
        public string SigningKey { get; set; } = string.Empty;
        public int AccessTokenMinutes { get; set; } = 30;
        public int RefreshTokenDays { get; set; } = 14;
        /// <summary>Lifetime of register-scoped PIN-exchange tokens (research R3).</summary>
        public int RegisterTokenMinutes { get; set; } = 720;
    }
}
