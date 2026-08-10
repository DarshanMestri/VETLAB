namespace VetLims.Infrastructure.Entities;

public class RegistrationAnimal : BaseEntity
{
    public int RegistrationId { get; set; }
    public Registration Registration { get; set; } = default!;
    public int? AnimalId { get; set; }
    public Animal? Animal { get; set; }
    public int SpeciesId { get; set; }
    public Species Species { get; set; } = default!;
    public string AnimalName { get; set; } = default!;
    public int? AgeInDays { get; set; }
}
