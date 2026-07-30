namespace InventoryX.Presentation.Swagger;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class LiveOnlyAttribute(string? reason = null) : Attribute
{
    public string? Reason { get; } = reason;
}
