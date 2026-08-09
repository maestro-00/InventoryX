namespace InventoryX.Application.Services.IServices
{
    public record TaxComponentResult(string Code, string Name, decimal Rate, decimal Amount);

    /// <summary>
    /// Computes tax component amounts for a net line amount from a
    /// TaxTreatment components JSON definition (research R11): levy components
    /// apply to the net base; VAT compounds on net + levies per GRA rules.
    /// </summary>
    public interface ITaxCalculator
    {
        IReadOnlyList<TaxComponentResult> Calculate(decimal netAmount, string componentsJson);
    }
}
