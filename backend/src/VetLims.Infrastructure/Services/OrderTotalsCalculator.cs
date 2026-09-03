using VetLims.Infrastructure.Entities;

namespace VetLims.Infrastructure.Services;

// A single priced line item.
public sealed record OrderLinePricing(decimal UnitPrice, decimal VatRate, decimal VatAmount, int Quantity);

// Result of an order-total calculation.
public sealed record OrderTotals(decimal TotalNet, decimal TotalVat, decimal TotalAmount, decimal DiscountAmount);

/// <summary>
/// Pure, DB-free order total calculation. Mirrors the server-authoritative math used
/// when creating a registration so that edits stay consistent with creates.
///
/// <paramref name="baseNet"/>/<paramref name="baseVat"/> represent pre-existing line items
/// whose prices were already fixed (0 in create mode). Only <paramref name="lines"/> are
/// re-priced. Discount is applied to the full (base + new) VAT-inclusive total.
/// </summary>
public static class OrderTotalsCalculator
{
    public static OrderTotals Compute(
        decimal baseNet,
        decimal baseVat,
        IEnumerable<OrderLinePricing> lines,
        DiscountType? discountType,
        decimal discountValue)
    {
        decimal addedNet = 0m, addedVat = 0m;
        foreach (var line in lines)
        {
            if (line.Quantity <= 0) continue;
            addedNet += line.UnitPrice * line.Quantity;
            addedVat += line.VatAmount * line.Quantity;
        }

        var totalNet = baseNet + addedNet;
        var totalVat = baseVat + addedVat;
        var discountAmount = ComputeDiscount(totalNet + totalVat, discountType, discountValue);
        var totalAmount = decimal.Round(totalNet + totalVat - discountAmount, 2, MidpointRounding.AwayFromZero);
        return new OrderTotals(decimal.Round(totalNet, 2, MidpointRounding.AwayFromZero), decimal.Round(totalVat, 2, MidpointRounding.AwayFromZero), totalAmount, discountAmount);
    }

    private static decimal ComputeDiscount(decimal subtotalPlusVat, DiscountType? discountType, decimal discountValue)
    {
        if (!discountType.HasValue || discountValue <= 0)
            return 0m;

        decimal discountAmount = 0m;
        if (discountType == DiscountType.Percentage)
        {
            discountAmount = discountValue > 100
                ? subtotalPlusVat
                : decimal.Round(subtotalPlusVat * discountValue / 100m, 2, MidpointRounding.AwayFromZero);
        }
        else if (discountType == DiscountType.Amount)
        {
            discountAmount = decimal.Round(discountValue, 2, MidpointRounding.AwayFromZero);
            if (discountAmount > subtotalPlusVat)
                discountAmount = subtotalPlusVat;
        }

        return discountAmount < 0 ? 0m : discountAmount;
    }
}
