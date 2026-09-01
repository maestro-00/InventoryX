namespace InventoryX.Presentation.Health
{
    /// <summary>Tracks whether startup migrations and seed completed successfully.</summary>
    public static class DatabaseStartupHealth
    {
        public static bool IsHealthy { get; set; } = true;
    }
}
