using System.Text.Json;
using InventoryX.Application.Services.IServices;

namespace InventoryX.Application.Services
{
    public class TaxCalculator : ITaxCalculator
    {
        private sealed record ComponentDef(string Code, string? Name, decimal Rate, string? Base);

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public IReadOnlyList<TaxComponentResult> Calculate(decimal netAmount, string componentsJson)
        {
            if (string.IsNullOrWhiteSpace(componentsJson)) return [];
            var definitions = JsonSerializer.Deserialize<List<ComponentDef>>(componentsJson, SerializerOptions) ?? [];
            if (definitions.Count == 0) return [];

            var results = new List<TaxComponentResult>();
            var leviesTotal = 0m;

            foreach (var definition in definitions.Where(d => d.Base is null or "net"))
            {
                var amount = Math.Round(netAmount * definition.Rate, 4);
                leviesTotal += amount;
                results.Add(new TaxComponentResult(definition.Code, definition.Name ?? definition.Code, definition.Rate, amount));
            }

            foreach (var definition in definitions.Where(d => d.Base == "net_plus_levies"))
            {
                var amount = Math.Round((netAmount + leviesTotal) * definition.Rate, 4);
                results.Add(new TaxComponentResult(definition.Code, definition.Name ?? definition.Code, definition.Rate, amount));
            }

            return results;
        }
    }
}
