namespace VetLims.Infrastructure.Entities;

public enum LocationType
{
    MainLaboratory = 1,
    CollectionCenter = 2,
    HospitalLaboratory = 3,
    SatelliteLaboratory = 4,
    MobileLaboratory = 5
}

public class Branch : BaseEntity
{
    public int ClinicId { get; set; }
    public Clinic Clinic { get; set; } = default!;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public LocationType LocationType { get; set; } = LocationType.MainLaboratory;

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? Pin { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Manager { get; set; }

    public bool IsActive { get; set; } = true;
}
