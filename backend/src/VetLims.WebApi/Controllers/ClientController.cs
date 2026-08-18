using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.WebApi.DTOs;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;

namespace VetLims.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly VetLimsDbContext _db;
    public ClientsController(VetLimsDbContext db) => _db = db;

    [HttpPost]
    public async Task<ActionResult<ClientDto>> Create([FromBody] ClientCreateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var name = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Client name is required." });

        var code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim();

        if (code != null)
        {
            var exists = await _db.Clients.AnyAsync(c => c.Code == code && !c.IsDeleted);
            if (exists)
                return Conflict(new { message = $"A client with code '{code}' already exists." });
        }

        var entity = new Client
        {
            Name = name,
            Code = code,
            Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim(),
            IsActive = dto.IsActive
        };

        _db.Clients.Add(entity);
        await _db.SaveChangesAsync();

        var result = ToDto(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        IQueryable<Client> q = _db.Clients.AsNoTracking().Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(c =>
                EF.Functions.ILike(c.Name, $"%{s}%") ||
                EF.Functions.ILike(c.Code ?? "", $"%{s}%") ||
                EF.Functions.ILike(c.Email ?? "", $"%{s}%") ||
                EF.Functions.ILike(c.Phone ?? "", $"%{s}%")
            );
        }

        var items = await q
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => ToDto(c))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClientDto>> GetById(int id)
    {
        var e = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (e is null) return NotFound();
        return Ok(ToDto(e));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClientDto>> Update(int id, [FromBody] ClientUpdateDto dto)
    {
        var e = await _db.Clients.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (e is null) return NotFound();

        var name = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Client name is required." });

        var code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim();

        if (code != null)
        {
            var exists = await _db.Clients.AnyAsync(c => c.Code == code && c.Id != id && !c.IsDeleted);
            if (exists)
                return Conflict(new { message = $"A client with code '{code}' already exists." });
        }

        e.Name = name;
        e.Code = code;
        e.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        e.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        e.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
        e.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return Ok(ToDto(e));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] bool isActive)
    {
        var e = await _db.Clients.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (e is null) return NotFound();
        e.IsActive = isActive;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ClientDto ToDto(Client c) =>
        new ClientDto(c.Id, c.Name, c.Code, c.Phone, c.Email, c.Address, c.IsActive);
}
