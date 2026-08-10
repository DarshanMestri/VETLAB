// Step 1: ClientsController — create/read/update/delete Clients
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.WebApi.DTOs;
using VetLims.Infrastructure;                 // AppDbContext (adjust if different)
using VetLims.Infrastructure.Entities;        // Client entity

namespace VetLims.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly VetLimsDbContext _db;
    public ClientsController(VetLimsDbContext db) => _db = db;

    // POST /api/clients
    [HttpPost]
    public async Task<ActionResult<ClientDto>> Create([FromBody] ClientCreateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var entity = new Client
        {
            Name = dto.Name.Trim(),
            Code = dto.Code?.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            IsActive = dto.IsActive
        };

        _db.Clients.Add(entity);
        await _db.SaveChangesAsync();

        var result = ToDto(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
    }

    // GET /api/clients
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        IQueryable<Client> q = _db.Clients.AsNoTracking();

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

    // GET /api/clients/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClientDto>> GetById(int id)
    {
        var e = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return NotFound();
        return Ok(ToDto(e));
    }

    // PUT /api/clients/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClientDto>> Update(int id, [FromBody] ClientUpdateDto dto)
    {
        var e = await _db.Clients.FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return NotFound();

        e.Name = dto.Name.Trim();
        e.Code = dto.Code?.Trim();
        e.Phone = dto.Phone?.Trim();
        e.Email = dto.Email?.Trim();
        e.Address = dto.Address?.Trim();
        e.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return Ok(ToDto(e));
    }

    // DELETE /api/clients/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Clients.FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return NotFound();

        _db.Clients.Remove(e);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- mapping helper ---
    private static ClientDto ToDto(Client c) =>
        new ClientDto(c.Id, c.Name, c.Code, c.Phone, c.Email, c.Address, c.IsActive);
}
