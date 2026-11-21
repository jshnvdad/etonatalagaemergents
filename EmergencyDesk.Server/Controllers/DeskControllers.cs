using EmergencyDesk.Server.Data;
using EmergencyDesk.Server.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmergencyDesk.Server.Controllers;

/// <summary>Endpoints used by the WPF desk app (Admin + Dispatcher).</summary>
[ApiController]
[Route("api/incidents")]
[Authorize(Roles = "Admin,Dispatcher")]
public class IncidentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public IncidentsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.Incidents
            .Include(i => i.User).Include(i => i.AssignedResponder)
            .OrderByDescending(i => i.ReportedAt)
            .Take(500)
            .ToListAsync();

        return Ok(list.Select(i => new DeskIncidentDto(
            i.Id, i.Type, i.Status, i.Description,
            i.User?.FullName ?? "", i.User?.Phone ?? "",
            i.Latitude ?? 0, i.Longitude ?? 0, i.Address,
            DateTime.SpecifyKind(i.ReportedAt, DateTimeKind.Utc),
            i.AssignedResponderId, i.AssignedResponder?.Name)));
    }

    [HttpPut("{id:int}/assign")]
    public async Task<IActionResult> Assign(int id, AssignRequest r)
    {
        var incident = await _db.Incidents.FindAsync(id);
        var responder = await _db.Responders.FindAsync(r.ResponderId);
        if (incident is null || responder is null) return NotFound();

        // Free the previously assigned responder, if any.
        if (incident.AssignedResponderId is int oldId && oldId != responder.Id)
        {
            var old = await _db.Responders.FindAsync(oldId);
            if (old is not null && old.Status == ResponderStatus.Busy) old.Status = ResponderStatus.Available;
        }

        incident.AssignedResponderId = responder.Id;
        if (incident.Status == IncidentStatus.Received) incident.Status = IncidentStatus.Dispatched;
        responder.Status = ResponderStatus.Busy;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, StatusRequest r)
    {
        var incident = await _db.Incidents.Include(i => i.AssignedResponder).FirstOrDefaultAsync(i => i.Id == id);
        if (incident is null) return NotFound();

        incident.Status = r.Status;
        if (r.Status is IncidentStatus.Resolved or IncidentStatus.Cancelled
            && incident.AssignedResponder is { Status: ResponderStatus.Busy } responder)
            responder.Status = ResponderStatus.Available;

        await _db.SaveChangesAsync();
        return Ok();
    }
}

[ApiController]
[Route("api/responders")]
[Authorize(Roles = "Admin,Dispatcher")]
public class RespondersController : ControllerBase
{
    private readonly AppDbContext _db;
    public RespondersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Responders.OrderBy(r => r.Name)
            .Select(r => new ResponderDto(r.Id, r.Name, r.Unit, r.Specialty, r.Status, r.Phone))
            .ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(ResponderDto d)
    {
        if (string.IsNullOrWhiteSpace(d.Name) || string.IsNullOrWhiteSpace(d.Unit))
            return BadRequest("Name and unit are required.");
        var r = new Responder { Name = d.Name, Unit = d.Unit, Specialty = d.Specialty, Status = d.Status, Phone = d.Phone };
        _db.Responders.Add(r);
        await _db.SaveChangesAsync();
        return Ok(new ResponderDto(r.Id, r.Name, r.Unit, r.Specialty, r.Status, r.Phone));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ResponderDto d)
    {
        var r = await _db.Responders.FindAsync(id);
        if (r is null) return NotFound();
        r.Name = d.Name; r.Unit = d.Unit; r.Specialty = d.Specialty; r.Status = d.Status; r.Phone = d.Phone;
        await _db.SaveChangesAsync();
        return Ok();
    }
}

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok((await _db.AppUsers.OrderBy(u => u.FullName).ToListAsync())
            .Select(u => new DeskUserDto(u.Id, u.FullName, u.Email, u.Phone, u.IsActive,
                DateTime.SpecifyKind(u.RegisteredAt, DateTimeKind.Utc))));

    [HttpPut("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, ActiveRequest r)
    {
        var u = await _db.AppUsers.FindAsync(id);
        if (u is null) return NotFound();
        u.IsActive = r.IsActive;
        await _db.SaveChangesAsync();
        return Ok();
    }
}
