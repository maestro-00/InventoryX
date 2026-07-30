using FluentAssertions;
namespace InventoryX.Application.Tests.Purchasing;
public sealed class GoodsReceiptTests { [Fact] public void Goods_receipt_contract_exists() => Type.GetType("InventoryX.Domain.Models.Purchasing.GoodsReceipt, InventoryX.Domain").Should().NotBeNull("short, over and damaged deliveries must retain PO state"); }
