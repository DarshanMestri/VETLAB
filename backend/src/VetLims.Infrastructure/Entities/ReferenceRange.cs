namespace VetLims.Infrastructure.Entities;

public class ReferenceRange
{
    public int Id { get; set; }
    public int TestParameterId { get; set; }
    public TestParameter? TestParameter { get; set; }

    public int SpeciesId { get; set; }
    public Species? Species { get; set; }

    // Age buckets (choose any subset – nullable means “not bounded”)
    public int? MinDays { get; set; }      // inclusive
    public int? MaxDays { get; set; }      // inclusive

    public decimal? Low { get; set; }
    public decimal? High { get; set; }
    public string? TextRange { get; set; } // for qualitative ranges if needed
}
