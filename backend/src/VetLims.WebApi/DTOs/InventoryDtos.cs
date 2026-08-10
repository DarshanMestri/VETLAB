namespace VetLims.WebApi.DTOs;

public class InventoryPurchaseListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public int? SupplierId { get; set; }
}

public class InventoryStockListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public bool LowStockOnly { get; set; }
    public int? ExpiryWithinDays { get; set; }
}

public record InventoryPurchaseLineUpsertDto(
    int MedicineId,
    int? InventoryCategoryId,
    string BatchNumber,
    DateOnly? ExpiryDate,
    decimal Quantity,
    decimal UnitCost,
    decimal LowStockThreshold
);

public record InventoryPurchaseCreateDto(
    DateTime? PurchaseDate,
    int SupplierId,
    string Currency,
    string? InvoiceReference,
    string? Notes,
    List<InventoryPurchaseLineUpsertDto> Lines
);

public record InventoryPurchaseReturnLineCreateDto(
    int PurchaseLineId,
    decimal Quantity,
    string? Reason
);

public record InventoryPurchaseReturnCreateDto(
    DateTime? ReturnDate,
    string? Notes,
    List<InventoryPurchaseReturnLineCreateDto> Lines
);

public record InventoryConsumptionCreateDto(
    decimal Quantity,
    DateTime? ConsumedAt,
    string? Notes
);

public record InventoryPurchaseListDto(
    int Id,
    string PurchaseNumber,
    DateTime PurchaseDate,
    int SupplierId,
    string SupplierName,
    string Currency,
    decimal TotalAmount
);

public record InventoryPurchaseLineDto(
    int Id,
    int MedicineId,
    string MedicineName,
    int? InventoryCategoryId,
    string? InventoryCategoryName,
    string BatchNumber,
    DateOnly? ExpiryDate,
    decimal Quantity,
    decimal UnitCost,
    decimal LowStockThreshold,
    decimal ReturnedQuantity,
    decimal ConsumedQuantity,
    decimal AvailableQuantity,
    decimal LineAmount
);

public record InventoryPurchaseReturnLineDto(
    int Id,
    int PurchaseLineId,
    decimal Quantity,
    decimal UnitCost,
    decimal ReturnAmount,
    string? Reason
);

public record InventoryPurchaseReturnDto(
    int Id,
    string ReturnNumber,
    DateTime ReturnDate,
    decimal TotalReturnAmount,
    string? Notes,
    IEnumerable<InventoryPurchaseReturnLineDto> Lines
);

public record InventoryPurchaseDetailDto(
    int Id,
    string PurchaseNumber,
    DateTime PurchaseDate,
    int SupplierId,
    string SupplierName,
    string Currency,
    string? InvoiceReference,
    string? Notes,
    decimal TotalAmount,
    IEnumerable<InventoryPurchaseLineDto> Lines,
    IEnumerable<InventoryPurchaseReturnDto> Returns
);

public record InventoryStockBatchDto(
    int PurchaseLineId,
    int PurchaseId,
    string PurchaseNumber,
    DateTime PurchaseDate,
    int MedicineId,
    string MedicineName,
    string BatchNumber,
    DateOnly? ExpiryDate,
    decimal Quantity,
    decimal ReturnedQuantity,
    decimal ConsumedQuantity,
    decimal AvailableQuantity,
    decimal LowStockThreshold,
    bool IsLowStock,
    bool IsExpired
);

public record InventoryConsumptionDto(
    int Id,
    int PurchaseLineId,
    decimal Quantity,
    DateTime ConsumedAt,
    string? Notes
);
