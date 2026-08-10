namespace VetLims.Infrastructure.Entities;

public class InventoryPurchase : BaseEntity
{
    public string PurchaseNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;
    public string Currency { get; set; } = "AED";
    public string? InvoiceReference { get; set; }
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<InventoryPurchaseLine> Lines { get; set; } = new List<InventoryPurchaseLine>();
    public ICollection<InventoryPurchaseReturn> Returns { get; set; } = new List<InventoryPurchaseReturn>();
}

public class InventoryPurchaseLine : BaseEntity
{
    public int InventoryPurchaseId { get; set; }
    public InventoryPurchase InventoryPurchase { get; set; } = default!;
    public int MedicineId { get; set; }
    public Medicine Medicine { get; set; } = default!;
    public int? InventoryCategoryId { get; set; }
    public InventoryCategory? InventoryCategory { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LowStockThreshold { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal LineAmount { get; set; }

    public ICollection<InventoryConsumption> Consumptions { get; set; } = new List<InventoryConsumption>();
    public ICollection<InventoryPurchaseReturnLine> ReturnLines { get; set; } = new List<InventoryPurchaseReturnLine>();
}

public class InventoryPurchaseReturn : BaseEntity
{
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;
    public int InventoryPurchaseId { get; set; }
    public InventoryPurchase InventoryPurchase { get; set; } = default!;
    public string? Notes { get; set; }
    public decimal TotalReturnAmount { get; set; }

    public ICollection<InventoryPurchaseReturnLine> Lines { get; set; } = new List<InventoryPurchaseReturnLine>();
}

public class InventoryPurchaseReturnLine : BaseEntity
{
    public int InventoryPurchaseReturnId { get; set; }
    public InventoryPurchaseReturn InventoryPurchaseReturn { get; set; } = default!;
    public int InventoryPurchaseLineId { get; set; }
    public InventoryPurchaseLine InventoryPurchaseLine { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal ReturnAmount { get; set; }
    public string? Reason { get; set; }
}

public class InventoryConsumption : BaseEntity
{
    public int InventoryPurchaseLineId { get; set; }
    public InventoryPurchaseLine InventoryPurchaseLine { get; set; } = default!;
    public decimal Quantity { get; set; }
    public DateTime ConsumedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
