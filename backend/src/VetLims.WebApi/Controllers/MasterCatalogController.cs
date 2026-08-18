using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Controllers;

/// <summary>
/// Step 5: CRUD for existing, non-clinical master tables.
/// Species, departments and units intentionally retain their established dedicated endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class MasterCatalogController(VetLimsDbContext db) : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    [HttpGet("{type}")]
    public async Task<IActionResult> List(
        string type,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseType(type, out var masterType)) return NotFound(UnsupportedType(type));
        if (page < 1) return BadRequest("Page must be greater than zero.");
        if (pageSize is < 1 or > MaximumPageSize)
            return BadRequest($"Page size must be between 1 and {MaximumPageSize}.");

        var term = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
        var result = masterType switch
        {
            MasterType.Species => await PageAsync(db.Species, term, page, pageSize,
                x => new SpeciesDto(x.Id, x.Code, x.Name, x.IsActive), cancellationToken),
            MasterType.Departments => await PageAsync(db.Departments, term, page, pageSize,
                x => new DepartmentDto(x.Id, x.Name, x.Code, x.IsActive), cancellationToken),
            MasterType.Units => await PageUnitsAsync(term, page, pageSize, cancellationToken),
            MasterType.Breeds => await PageAsync(db.Breeds, term, page, pageSize,
                x => new BreedDto(x.Id, x.Code, x.Name, x.Description, x.IsActive), cancellationToken),
            MasterType.AnimalColors => await PageAsync(db.AnimalColors, term, page, pageSize,
                x => new AnimalColorDto(x.Id, x.Code, x.Name, x.Description, x.IsActive), cancellationToken),
            MasterType.Genders => await PageAsync(db.Genders, term, page, pageSize,
                x => new GenderDto(x.Id, x.Code, x.Name, x.Description, x.IsActive), cancellationToken),
            MasterType.Doctors => await PageAsync(db.Doctors, term, page, pageSize,
                x => new DoctorDto(x.Id, x.Code, x.Name, x.LicenseNumber, x.DepartmentId, x.Phone, x.Email, x.IsActive), cancellationToken),
            MasterType.SampleTypes => await PageAsync(db.SampleTypes, term, page, pageSize,
                x => new SampleTypeDto(x.Id, x.Code, x.Name, x.Description, x.IsActive), cancellationToken),
            MasterType.Containers => await PageAsync(db.Containers, term, page, pageSize,
                x => new ContainerDto(x.Id, x.Code, x.Name, x.Description, x.IsActive), cancellationToken),
            MasterType.Methods => await PageAsync(db.Methods, term, page, pageSize,
                x => new MethodDto(x.Id, x.Code, x.Name, x.Description, x.IsActive), cancellationToken),
            MasterType.Instruments => await PageAsync(db.Instruments, term, page, pageSize,
                x => new InstrumentDto(x.Id, x.Code, x.Name, x.Manufacturer, x.Model, x.IsActive), cancellationToken),
            MasterType.Suppliers => await PageAsync(db.Suppliers, term, page, pageSize,
                x => new SupplierDto(x.Id, x.Code, x.Name, x.ContactPerson, x.Phone, x.Email, x.Address, x.IsActive), cancellationToken),
            MasterType.Medicines => await PageAsync(db.Medicines, term, page, pageSize,
                x => new MedicineDto(x.Id, x.Code, x.Name, x.GenericName, x.Category, x.Unit, x.IsActive), cancellationToken),
            MasterType.InventoryCategories => await PageAsync(db.InventoryCategories, term, page, pageSize,
                x => new InventoryCategoryDto(x.Id, x.Code, x.Name, x.Description, x.IsActive), cancellationToken),
            _ => throw new InvalidOperationException("Unsupported master type.")
        };

        return Ok(result);
    }

    [HttpPost("{type}")]
    public async Task<IActionResult> Create(
        string type,
        [FromBody] MasterCatalogUpsertDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseType(type, out var masterType)) return NotFound(UnsupportedType(type));
        var validation = await ValidateAsync(masterType, dto, null, cancellationToken);
        if (validation is not null) return validation;

        var entity = CreateEntity(masterType, dto);
        db.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(List), new { type }, Map(masterType, entity));
    }

    [HttpPut("{type}/{id:int}")]
    public async Task<IActionResult> Update(
        string type,
        int id,
        [FromBody] MasterCatalogUpsertDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseType(type, out var masterType)) return NotFound(UnsupportedType(type));
        var entity = await FindAsync(masterType, id, cancellationToken);
        if (entity is null || entity.IsDeleted) return NotFound();

        var validation = await ValidateAsync(masterType, dto, id, cancellationToken);
        if (validation is not null) return validation;

        Apply(masterType, entity, dto);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(Map(masterType, entity));
    }

    [HttpPatch("{type}/{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(string type, int id, [FromBody] bool isActive, CancellationToken cancellationToken = default)
    {
        if (!TryParseType(type, out var masterType)) return NotFound(UnsupportedType(type));
        var entity = await FindAsync(masterType, id, cancellationToken);
        if (entity is null || entity.IsDeleted) return NotFound();

        var statusProperty = entity.GetType().GetProperty("IsActive");
        if (statusProperty is null || !statusProperty.CanWrite)
            return BadRequest(new { message = $"{masterType.ToDisplayName()} does not support active status." });

        statusProperty.SetValue(entity, isActive);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult?> ValidateAsync(MasterType type, MasterCatalogUpsertDto dto, int? currentId, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        var code = dto.Code?.Trim();
        var name = dto.Name?.Trim();
        if (type == MasterType.Departments && string.IsNullOrWhiteSpace(code))
            code = null;

        if (type != MasterType.Departments && string.IsNullOrWhiteSpace(code))
            errors[nameof(dto.Code)] = [type == MasterType.Units ? "Symbol is required." : "Code is required."];
        else if (code is not null && code.Length > 50)
            errors[nameof(dto.Code)] = [type == MasterType.Units ? "Symbol cannot exceed 50 characters." : "Code cannot exceed 50 characters."];
        if (string.IsNullOrWhiteSpace(name)) errors[nameof(dto.Name)] = ["Name is required."];
        else if (name.Length > 200) errors[nameof(dto.Name)] = ["Name cannot exceed 200 characters."];
        if (!string.IsNullOrWhiteSpace(dto.Email) && !new EmailAddressAttribute().IsValid(dto.Email))
            errors[nameof(dto.Email)] = ["Email is invalid."];
        if (type == MasterType.Units && name?.Length > 100)
            errors[nameof(dto.Name)] = ["Name cannot exceed 100 characters."];

        if (type == MasterType.Doctors && dto.DepartmentId.HasValue)
        {
            var departmentExists = await db.Departments.AnyAsync(x => x.Id == dto.DepartmentId && !x.IsDeleted, ct);
            if (!departmentExists) errors[nameof(dto.DepartmentId)] = ["The selected department does not exist."];
        }

        if (type == MasterType.Departments && await db.Departments.AnyAsync(x => x.Name == name && !x.IsDeleted && x.Id != currentId, ct))
            errors[nameof(dto.Name)] = ["Department name already exists."];
        if (type == MasterType.Units && await db.Units.AnyAsync(x => x.Name == name && !x.IsDeleted && x.Id != currentId, ct))
            errors[nameof(dto.Name)] = ["Unit name already exists."];

        if (errors.Count > 0)
            return new ObjectResult(new ValidationProblemDetails(errors)) { StatusCode = StatusCodes.Status400BadRequest };

        if (code is not null && await CodeExistsAsync(type, code, currentId, ct))
        {
            var fieldName = type == MasterType.Units ? "symbol" : "code";
            return Conflict(new { message = $"A {type.ToDisplayName()} with {fieldName} '{code}' already exists." });
        }

        return null;
    }

    private async Task<bool> CodeExistsAsync(MasterType type, string code, int? currentId, CancellationToken ct) => type switch
    {
        MasterType.Species => await db.Species.AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != currentId, ct),
        MasterType.Departments => await db.Departments.AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != currentId, ct),
        MasterType.Units => await db.Units.AnyAsync(x => x.Symbol == code && !x.IsDeleted && x.Id != currentId, ct),
        MasterType.Breeds => await db.Breeds.AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != currentId, ct),
        MasterType.AnimalColors => await db.AnimalColors.AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != currentId, ct),
        MasterType.Genders => await db.Genders.AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != currentId, ct),
        MasterType.Doctors => await db.Doctors.AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != currentId, ct),
        MasterType.SampleTypes => await db.SampleTypes.AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != currentId, ct),
        MasterType.Containers => await db.Containers.AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != currentId, ct),
        MasterType.Methods => await db.Methods.AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != currentId, ct),
        MasterType.Instruments => await db.Instruments.AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != currentId, ct),
        MasterType.Suppliers => await db.Suppliers.AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != currentId, ct),
        MasterType.Medicines => await db.Medicines.AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != currentId, ct),
        MasterType.InventoryCategories => await db.InventoryCategories.AnyAsync(x => x.Code == code && !x.IsDeleted && x.Id != currentId, ct),
        _ => false
    };

    private static async Task<object> PageAsync<TEntity, TDto>(DbSet<TEntity> set, string? term, int page, int pageSize, Func<TEntity, TDto> map, CancellationToken ct)
        where TEntity : BaseEntity
    {
        // Mapping happens after the bounded, ordered database query, avoiding reflection/client evaluation in SQL.
        var query = set.AsNoTracking().Where(x => !x.IsDeleted);
        if (term is not null)
            query = query.Where(x => EF.Property<string>(x, "Code") != null &&
                (EF.Functions.ILike(EF.Property<string>(x, "Code"), term) || EF.Functions.ILike(EF.Property<string>(x, "Name"), term)));
        var total = await query.CountAsync(ct);
        var entities = await query.OrderBy(x => EF.Property<string>(x, "Name")).ThenBy(x => EF.Property<string>(x, "Code"))
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new { total, items = entities.Select(map), page, pageSize };
    }

    private async Task<object> PageUnitsAsync(string? term, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Units.AsNoTracking().Where(x => !x.IsDeleted);
        if (term is not null)
            query = query.Where(x => EF.Functions.ILike(x.Name, term) || (x.Symbol != null && EF.Functions.ILike(x.Symbol, term)));

        var total = await query.CountAsync(ct);
        var entities = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Symbol)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UnitDto(x.Id, x.Name, x.Symbol, x.IsActive))
            .ToListAsync(ct);

        return new { total, items = entities, page, pageSize };
    }

    private async Task<BaseEntity?> FindAsync(MasterType type, int id, CancellationToken ct) => type switch
    {
        MasterType.Species => await db.Species.FirstOrDefaultAsync(x => x.Id == id, ct),
        MasterType.Departments => await db.Departments.FirstOrDefaultAsync(x => x.Id == id, ct),
        MasterType.Units => await db.Units.FirstOrDefaultAsync(x => x.Id == id, ct),
        MasterType.Breeds => await db.Breeds.FirstOrDefaultAsync(x => x.Id == id, ct),
        MasterType.AnimalColors => await db.AnimalColors.FirstOrDefaultAsync(x => x.Id == id, ct),
        MasterType.Genders => await db.Genders.FirstOrDefaultAsync(x => x.Id == id, ct),
        MasterType.Doctors => await db.Doctors.FirstOrDefaultAsync(x => x.Id == id, ct),
        MasterType.SampleTypes => await db.SampleTypes.FirstOrDefaultAsync(x => x.Id == id, ct),
        MasterType.Containers => await db.Containers.FirstOrDefaultAsync(x => x.Id == id, ct),
        MasterType.Methods => await db.Methods.FirstOrDefaultAsync(x => x.Id == id, ct),
        MasterType.Instruments => await db.Instruments.FirstOrDefaultAsync(x => x.Id == id, ct),
        MasterType.Suppliers => await db.Suppliers.FirstOrDefaultAsync(x => x.Id == id, ct),
        MasterType.Medicines => await db.Medicines.FirstOrDefaultAsync(x => x.Id == id, ct),
        MasterType.InventoryCategories => await db.InventoryCategories.FirstOrDefaultAsync(x => x.Id == id, ct),
        _ => null
    };

    private static BaseEntity CreateEntity(MasterType type, MasterCatalogUpsertDto dto)
    {
        BaseEntity entity = type switch
        {
            MasterType.Species => new Species(),
            MasterType.Departments => new Department(),
            MasterType.Units => new Unit(),
            MasterType.Breeds => new Breed(), MasterType.AnimalColors => new AnimalColor(), MasterType.Genders => new Gender(),
            MasterType.Doctors => new Doctor(), MasterType.SampleTypes => new SampleType(), MasterType.Containers => new Container(),
            MasterType.Methods => new Method(), MasterType.Instruments => new Instrument(), MasterType.Suppliers => new Supplier(),
            MasterType.Medicines => new Medicine(), MasterType.InventoryCategories => new InventoryCategory(), _ => throw new InvalidOperationException("Unsupported master type.")
        };
        Apply(type, entity, dto);
        return entity;
    }

    private static void Apply(MasterType type, BaseEntity entity, MasterCatalogUpsertDto dto)
    {
        var code = dto.Code.Trim(); var name = dto.Name.Trim();
        switch (entity)
        {
            case Species x:
                x.Code = code;
                x.Name = name;
                x.IsActive = dto.IsActive;
                break;
            case Department x:
                x.Code = Clean(dto.Code);
                x.Name = name;
                x.IsActive = dto.IsActive;
                break;
            case Unit x:
                x.Name = name;
                x.Symbol = Clean(dto.Code);
                x.IsActive = dto.IsActive;
                break;
            case Breed x: ApplyBasic(x, code, name, dto); break;
            case AnimalColor x: ApplyBasic(x, code, name, dto); break;
            case Gender x: ApplyBasic(x, code, name, dto); break;
            case SampleType x: ApplyBasic(x, code, name, dto); break;
            case Container x: ApplyBasic(x, code, name, dto); break;
            case Method x: ApplyBasic(x, code, name, dto); break;
            case InventoryCategory x: ApplyBasic(x, code, name, dto); break;
            case Doctor x: x.Code = code; x.Name = name; x.DepartmentId = dto.DepartmentId; x.LicenseNumber = Clean(dto.LicenseNumber); x.Phone = Clean(dto.Phone); x.Email = Clean(dto.Email); x.IsActive = dto.IsActive; break;
            case Instrument x: x.Code = code; x.Name = name; x.Manufacturer = Clean(dto.Manufacturer); x.Model = Clean(dto.Model); x.IsActive = dto.IsActive; break;
            case Supplier x: x.Code = code; x.Name = name; x.ContactPerson = Clean(dto.ContactPerson); x.Phone = Clean(dto.Phone); x.Email = Clean(dto.Email); x.Address = Clean(dto.Address); x.IsActive = dto.IsActive; break;
            case Medicine x: x.Code = code; x.Name = name; x.GenericName = Clean(dto.GenericName); x.Category = Clean(dto.Category); x.Unit = Clean(dto.Unit); x.IsActive = dto.IsActive; break;
            default: throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    private static void ApplyBasic(dynamic entity, string code, string name, MasterCatalogUpsertDto dto)
    { entity.Code = code; entity.Name = name; entity.Description = Clean(dto.Description); entity.IsActive = dto.IsActive; }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static object Map(MasterType type, BaseEntity entity) => entity switch
    {
        Species x => new SpeciesDto(x.Id, x.Code, x.Name, x.IsActive),
        Department x => new DepartmentDto(x.Id, x.Name, x.Code, x.IsActive),
        Unit x => new UnitDto(x.Id, x.Name, x.Symbol, x.IsActive),
        Breed x => new BreedDto(x.Id, x.Code, x.Name, x.Description, x.IsActive),
        AnimalColor x => new AnimalColorDto(x.Id, x.Code, x.Name, x.Description, x.IsActive),
        Gender x => new GenderDto(x.Id, x.Code, x.Name, x.Description, x.IsActive),
        Doctor x => new DoctorDto(x.Id, x.Code, x.Name, x.LicenseNumber, x.DepartmentId, x.Phone, x.Email, x.IsActive),
        SampleType x => new SampleTypeDto(x.Id, x.Code, x.Name, x.Description, x.IsActive),
        Container x => new ContainerDto(x.Id, x.Code, x.Name, x.Description, x.IsActive),
        Method x => new MethodDto(x.Id, x.Code, x.Name, x.Description, x.IsActive),
        Instrument x => new InstrumentDto(x.Id, x.Code, x.Name, x.Manufacturer, x.Model, x.IsActive),
        Supplier x => new SupplierDto(x.Id, x.Code, x.Name, x.ContactPerson, x.Phone, x.Email, x.Address, x.IsActive),
        Medicine x => new MedicineDto(x.Id, x.Code, x.Name, x.GenericName, x.Category, x.Unit, x.IsActive),
        InventoryCategory x => new InventoryCategoryDto(x.Id, x.Code, x.Name, x.Description, x.IsActive),
        _ => throw new ArgumentOutOfRangeException(nameof(entity))
    };

    private static bool TryParseType(string value, out MasterType type) => Enum.TryParse(value.Replace("-", ""), true, out type);
    private static object UnsupportedType(string type) => new { message = $"Unsupported master type: {type}." };

    private enum MasterType
    {
        Species,
        Departments,
        Units,
        Breeds,
        AnimalColors,
        Genders,
        Doctors,
        SampleTypes,
        Containers,
        Methods,
        Instruments,
        Suppliers,
        Medicines,
        InventoryCategories
    }
}

internal static class MasterTypeExtensions
{
    public static string ToDisplayName(this Enum value) => value.ToString().Replace("Categories", " Categories").Replace("Colors", " Colors").Replace("Types", " Types");
}
