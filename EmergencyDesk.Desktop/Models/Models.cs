using System.Text.Json.Serialization;

namespace EmergencyDesk.Models;

public enum IncidentStatus { Received, Dispatched, EnRoute, OnScene, Resolved, Cancelled }
public enum IncidentType { Medical, Fire, Police, Rescue }
public enum ResponderStatus { Available, Busy, OffDuty }

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginResponse
{
    public string Token { get; set; } = "";
    public string DisplayName { get; set; } = "";
    /// <summary>"Admin" or "Dispatcher"</summary>
    public string Role { get; set; } = "";
}

public class Incident
{
    public int Id { get; set; }
    public IncidentType Type { get; set; }
    public IncidentStatus Status { get; set; }
    public string Description { get; set; } = "";
    public string ReporterName { get; set; } = "";
    public string ReporterPhone { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public DateTime ReportedAt { get; set; }
    public int? AssignedResponderId { get; set; }
    public string? AssignedResponderName { get; set; }

    [JsonIgnore] public string Coordinates => $"{Latitude:F5}, {Longitude:F5}";
    [JsonIgnore] public string ReportedLocal => ReportedAt.ToLocalTime().ToString("g");
}

public class Responder
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";      // e.g. "Ambulance 3", "Engine 12"
    public IncidentType Specialty { get; set; }
    public ResponderStatus Status { get; set; }
    public string Phone { get; set; } = "";
}

public class AppUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime RegisteredAt { get; set; }

    [JsonIgnore] public string RegisteredLocal => RegisteredAt.ToLocalTime().ToString("d");
}

public class AssignRequest { public int ResponderId { get; set; } }
public class StatusRequest { public IncidentStatus Status { get; set; } }
public class ActiveRequest { public bool IsActive { get; set; } }
