namespace InventoryX.Application.DTOs.Sales;

public record GetSaleStatsDto(int TotalLowStock, decimal TotalRevenue, decimal TotalTodaySales, int TotalInventoryItems);
