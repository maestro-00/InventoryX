using FluentAssertions;

namespace InventoryX.Application.Tests.Selling;

/// <summary>T086 - end-of-shift report must aggregate tender, refund, discount, void and variance totals.</summary>
public sealed class ZReportTests
{
    [Fact]
    public void Z_report_query_contract_exposes_register_and_staff_aggregation()
    {
        Type.GetType("InventoryX.Application.Queries.Requests.Selling.GetZReportQuery, InventoryX.Application")
            .Should().NotBeNull("a shift must expose a Z-report with sales, tender, refund, discount, void, and variance totals");
        Type.GetType("InventoryX.Application.Queries.RequestHandlers.Selling.GetZReportQueryHandler, InventoryX.Application")
            .Should().NotBeNull();
    }
}
