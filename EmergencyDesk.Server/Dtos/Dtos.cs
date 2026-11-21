using EmergencyDesk.Server.Data;

namespace EmergencyDesk.Server.Dtos;

// ---------- Mobile app (matches EmergencyApp.User/Models/Models.cs) ----------
public record MobileLoginRequest(string Email, string Password);
public record MobileRegisterRequest(string FullName, string Phone, string Email, string Password);
public record MobileAuthResponse(int UserId, string FullName, string Phone, string Email, string Token);
public record CreateEmergencyDto(string ClientId, int UserId, string Type, string Description,
                                 double? Latitude, double? Longitude, DateTime CreatedAtUtc);
public record MobileEmergencyDto(int ServerId, string ClientId, string Status, string? ResponderName);

// ---------- Desktop app (matches EmergencyDesk/Models/Models.cs) ----------
public record StaffLoginRequest(string Username, string Password);
public record StaffLoginResponse(string Token, string DisplayName, string Role);

public record DeskIncidentDto(
    int Id, IncidentType Type, IncidentStatus Status, string Description,
    string ReporterName, string ReporterPhone, double Latitude, double Longitude,
    string? Address, DateTime ReportedAt, int? AssignedResponderId, string? AssignedResponderName);

public record ResponderDto(int Id, string Name, string Unit, IncidentType Specialty,
                           ResponderStatus Status, string Phone);

public record DeskUserDto(int Id, string FullName, string Email, string Phone,
                          bool IsActive, DateTime RegisteredAt);

public record AssignRequest(int ResponderId);
public record StatusRequest(IncidentStatus Status);
public record ActiveRequest(bool IsActive);
