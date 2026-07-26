using InventoryX.Domain.Models.Catalog;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds Ghana tax treatments (T024, research R11): levies (NHIL 2.5%,
    /// GETFund 2.5%, COVID-19 HRL 1%) on the net amount, VAT 15% compounded on
    /// net + levies per GRA rules.
    /// </summary>
    public static class TaxSeeder
    {
        public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
        {
            var treatments = new List<TaxTreatment>
            {
                new()
                {
                    Code = "GH-STD",
                    Name = "Ghana Standard Rate",
                    CountryCode = "GH",
                    ComponentsJson =
                        """
                        [{"code":"NHIL","name":"National Health Insurance Levy","rate":0.025,"base":"net"},
                         {"code":"GETFUND","name":"GETFund Levy","rate":0.025,"base":"net"},
                         {"code":"COVID","name":"COVID-19 Health Recovery Levy","rate":0.01,"base":"net"},
                         {"code":"VAT","name":"Value Added Tax","rate":0.15,"base":"net_plus_levies"}]
                        """,
                },
                new()
                {
                    Code = "GH-ZERO",
                    Name = "Ghana Zero Rated",
                    CountryCode = "GH",
                    ComponentsJson = """[{"code":"VAT","name":"Value Added Tax","rate":0.0,"base":"net"}]""",
                },
                new()
                {
                    Code = "GH-EXEMPT",
                    Name = "Ghana Exempt",
                    CountryCode = "GH",
                    ComponentsJson = "[]",
                },
            };

            foreach (var treatment in treatments)
            {
                if (!await context.TaxTreatments.AnyAsync(t => t.Code == treatment.Code, cancellationToken))
                    context.TaxTreatments.Add(treatment);
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
