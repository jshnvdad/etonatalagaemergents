using SQLite;

namespace EmergencyApp.User.Models;

public static class RequestStatus
{
    public const string Pending = "Pending";       // saved on device, not yet on server
    public const string Received = "Received";     // server has it
    public const string Dispatched = "Dispatched"; // a unit is on the way
    public const string Resolved = "Resolved";
    public const string Cancelled = "Cancelled";
}

public class LocalUser
{
    [PrimaryKey] public int Id { get; set; }   // server-side user id
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string BloodType { get; set; } = "";
    public string Allergies { get; set; } = "";
    public string Conditions { get; set; } = "";
}

public class EmergencyContact
{
    [PrimaryKey, AutoIncrement] public int LocalId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
}

public class EmergencyRequest
{
    [PrimaryKey, AutoIncrement] public int LocalId { get; set; }
    [Indexed] public string ClientId { get; set; } = Guid.NewGuid().ToString();
    public int UserId { get; set; }
    public int? ServerId { get; set; }
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Status { get; set; } = RequestStatus.Pending;
    public string? ResponderName { get; set; }
    public bool IsSynced { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // UI helpers (not stored)
    [Ignore] public string CreatedLocal => CreatedAtUtc.ToLocalTime().ToString("dd MMM yyyy, HH:mm");
    [Ignore] public string Icon => EmergencyTypeOption.All.FirstOrDefault(t => t.Name == Type)?.Icon ?? "🚨";
    [Ignore] public string LocationText => Latitude is null ? "Location unavailable" : $"{Latitude:F5}, {Longitude:F5}";
    [Ignore] public Color StatusColor => Status switch
    {
        RequestStatus.Pending => Colors.Orange,
        RequestStatus.Received => Colors.DodgerBlue,
        RequestStatus.Dispatched => Colors.Purple,
        RequestStatus.Resolved => Colors.SeaGreen,
        _ => Colors.Gray
    };
}

public class EmergencyTypeOption
{
    public string Name { get; init; } = "";
    public string Icon { get; init; } = "";

    public static readonly List<EmergencyTypeOption> All = new()
    {
        new() { Name = "Medical", Icon = "🚑" },
        new() { Name = "Fire",    Icon = "🔥" },
        new() { Name = "Police",  Icon = "🚓" },
        new() { Name = "Rescue",  Icon = "🆘" },
    };
}

// ---- API DTOs (keep in sync with the ASP.NET Core server) ----
public record LoginRequest(string Email, string Password);
public record RegisterRequest(string FullName, string Phone, string Email, string Password);
public record AuthResponse(int UserId, string FullName, string Phone, string Email, string Token);
public record CreateEmergencyDto(string ClientId, int UserId, string Type, string Description,
                                 double? Latitude, double? Longitude, DateTime CreatedAtUtc);
public record EmergencyDto(int ServerId, string ClientId, string Status, string? ResponderName);
public record ApiResult<T>(bool Success, T? Data, string? Error, bool Offline = false);
