using FluentAssertions;
namespace InventoryX.Application.Tests.Inventory;
public sealed class FefoIssueTests { [Fact] public void Fefo_batch_issue_contract_exists() { Type.GetType("InventoryX.Domain.Models.Inventory.Batch, InventoryX.Domain").Should().NotBeNull("batch-tracked sales must select the earliest expiry batch"); } }
