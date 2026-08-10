namespace VetLims.Infrastructure.Entities;

public class BarcodeSequence : BaseEntity
{
    public string Prefix { get; set; } = "VL";
    public int PadLength { get; set; } = 6;
    public long CurrentValue { get; set; } = 0; // next is CurrentValue + 1
    public bool IsActive { get; set; } = true;
}
