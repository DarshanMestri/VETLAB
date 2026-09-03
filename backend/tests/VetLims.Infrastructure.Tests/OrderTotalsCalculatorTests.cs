using VetLims.Infrastructure.Entities;
using VetLims.Infrastructure.Services;
using Xunit;

namespace VetLims.Infrastructure.Tests;

public sealed class OrderTotalsCalculatorTests
{
    private static OrderLinePricing Line(decimal unitPrice, decimal vatRate, decimal vatAmount, int qty = 1)
        => new(unitPrice, vatRate, vatAmount, qty);

    [Fact]
    public void Compute_with_only_existing_base_keeps_totals_unchanged()
    {
        var totals = OrderTotalsCalculator.Compute(
            baseNet: 200m,
            baseVat: 10m,
            lines: Array.Empty<OrderLinePricing>(),
            discountType: null,
            discountValue: 0);

        Assert.Equal(200m, totals.TotalNet);
        Assert.Equal(10m, totals.TotalVat);
        Assert.Equal(210m, totals.TotalAmount);
        Assert.Equal(0m, totals.DiscountAmount);
    }

    [Fact]
    public void Compute_existing_plus_new_line_sums_base_and_latest_price_delta()
    {
        // Existing base = 100 net + 5 vat. New added line priced at the latest rate.
        var totals = OrderTotalsCalculator.Compute(
            baseNet: 100m,
            baseVat: 5m,
            lines: new[] { Line(unitPrice: 50m, vatRate: 5, vatAmount: 2.5m, qty: 1) },
            discountType: null,
            discountValue: 0);

        Assert.Equal(150m, totals.TotalNet);       // 100 + 50
        Assert.Equal(7.5m, totals.TotalVat);       // 5 + 2.5
        Assert.Equal(157.5m, totals.TotalAmount);  // 150 + 7.5
        Assert.Equal(0m, totals.DiscountAmount);
    }

    [Fact]
    public void Compute_quantity_scales_unit_price_for_new_lines()
    {
        var totals = OrderTotalsCalculator.Compute(
            baseNet: 0m,
            baseVat: 0m,
            lines: new[] { Line(unitPrice: 40m, vatRate: 5, vatAmount: 2m, qty: 3) },
            discountType: null,
            discountValue: 0);

        Assert.Equal(120m, totals.TotalNet);       // 40 * 3
        Assert.Equal(6m, totals.TotalVat);         // 2 * 3
        Assert.Equal(126m, totals.TotalAmount);
    }

    [Fact]
    public void Compute_percentage_discount_rounds_away_from_zero()
    {
        var totals = OrderTotalsCalculator.Compute(
            baseNet: 100m,
            baseVat: 0m,
            lines: new[] { Line(unitPrice: 33.33m, vatRate: 0, vatAmount: 0m, qty: 1) },
            discountType: DiscountType.Percentage,
            discountValue: 10m);

        // subtotal+vAT = 133.33 ; 10% = 13.333 -> rounds to 13.33
        Assert.Equal(13.33m, totals.DiscountAmount);
        Assert.Equal(120m, totals.TotalAmount);    // 133.33 - 13.33 = 120.00
    }

    [Fact]
    public void Compute_amount_discount_caps_at_subtotal()
    {
        var totals = OrderTotalsCalculator.Compute(
            baseNet: 50m,
            baseVat: 0m,
            lines: Array.Empty<OrderLinePricing>(),
            discountType: DiscountType.Amount,
            discountValue: 999m);

        Assert.Equal(50m, totals.DiscountAmount);
        Assert.Equal(0m, totals.TotalAmount);
    }

    [Fact]
    public void Compute_negative_discount_is_clamped_to_zero()
    {
        var totals = OrderTotalsCalculator.Compute(
            baseNet: 50m,
            baseVat: 0m,
            lines: Array.Empty<OrderLinePricing>(),
            discountType: DiscountType.Amount,
            discountValue: -5m);

        Assert.Equal(0m, totals.DiscountAmount);
        Assert.Equal(50m, totals.TotalAmount);
    }

    [Fact]
    public void Compute_zero_quantity_lines_are_ignored()
    {
        var totals = OrderTotalsCalculator.Compute(
            baseNet: 0m,
            baseVat: 0m,
            lines: new[] { Line(unitPrice: 100m, vatRate: 5, vatAmount: 5m, qty: 0) },
            discountType: null,
            discountValue: 0);

        Assert.Equal(0m, totals.TotalNet);
        Assert.Equal(0m, totals.TotalVat);
        Assert.Equal(0m, totals.TotalAmount);
    }
}
