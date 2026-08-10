namespace VetLims.Infrastructure.Entities;

public class Species : BaseEntity
{
    public string Name { get; set; } = default!; // Any language
    public bool IsActive { get; set; } = true;
    public string Code { get; set; } = default!;

}
