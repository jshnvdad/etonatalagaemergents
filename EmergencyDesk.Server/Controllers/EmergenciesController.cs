using System.Security.Claims;
using EmergencyDesk.Server.Data;
using EmergencyDesk.Server.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmergencyDesk.Server.Controllers;

/// <summary>Endpoints used by the mobile app.</summary>
[ApiController]
[Route("api/emergencies")]
[Authorize(Roles = "User")]
public class EmergenciesController : ControllerBase
{
    private readonly AppDbContext _db;
    public EmergenciesController(AppDbContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Create(CreateEmergencyDto d)
    {
        var userId = CurrentUserId;   // trust the token, not the body
        var user = await _db.AppUsers.FindAsync(userId);
        if (user is null) return Unauthorized();
        if (!user.IsActive) return StatusCode(403, "This account has been deactivated.");

        // Idempotency: a retried upload returns the existing record.
        var existing = await _db.Incidents.Include(i => i.AssignedResponder)
            .FirstOrDefaultAsync(i => i.ClientId == d.ClientId && i.UserId == userId);
        if (existing is not null) return Ok(ToMobile(existing));

        if (!Enum.TryParse<IncidentType>(d.Type, ignoreCase: true, out var type))
            return BadRequest("Unknown emergency type.");

        var incident = new Incident
        {
            ClientId = d.ClientId,
            UserId = userId,
            Type = type,
            Description = d.Description ?? "",
            Latitude = d.Latitude,
            Longitude = d.Longitude,
            Address = d.Latitude is null ? "Location unavailable" : null,
            ReportedAt = DateTime.SpecifyKind(d.CreatedAtUtc, DateTimeKind.Utc),
            Status = IncidentStatus.Received
        };
        _db.Incidents.Add(incident);
        await _db.SaveChangesAsync();
        return Ok(ToMobile(incident));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        var userId = CurrentUserId;
        var list = await _db.Incidents.Include(i => i.AssignedResponder)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.ReportedAt)
            .ToListAsync();
        return Ok(list.Select(ToMobile));
    }

    /// The phone only knows Received / Dispatched / Resolved / Cancelled,
    /// so the finer desk statuses are shown to the user as "Dispatched".
    private static MobileEmergencyDto ToMobile(Incident i)
    {
        var status = i.Status is IncidentStatus.EnRoute or IncidentStatus.OnScene
            ? IncidentStatus.Dispatched : i.Status;
        return new(i.Id, i.ClientId, status.ToString(), i.AssignedResponder?.Name);
    }
}
