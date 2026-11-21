using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EmergencyDesk.Models;

namespace EmergencyDesk.Services;

public class AppSettings
{
    public string ApiBaseUrl { get; set; } = "https://localhost:7001/";
    public int RefreshSeconds { get; set; } = 5;

    public static AppSettings Load()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(path))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings();
        }
        catch { /* fall back to defaults */ }
        return new AppSettings();
    }
}

public class SessionService
{
    public string Token { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsAdmin => Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    public void Clear() { Token = DisplayName = Role = ""; }
}

/// <summary>Thin REST client for the ASP.NET Core server.</summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly SessionService _session;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiClient(AppSettings settings, SessionService session)
    {
        _session = session;
        _http = new HttpClient { BaseAddress = new Uri(settings.ApiBaseUrl), Timeout = TimeSpan.FromSeconds(15) };
    }

    private void ApplyAuth() =>
        _http.DefaultRequestHeaders.Authorization = _session.IsLoggedIn
            ? new AuthenticationHeaderValue("Bearer", _session.Token) : null;

    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        var res = await _http.PostAsJsonAsync("api/staff/login",
            new LoginRequest { Username = username, Password = password }, Json);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<LoginResponse>(Json);
    }

    public async Task<List<Incident>> GetIncidentsAsync() => await GetAsync<List<Incident>>("api/incidents") ?? new();
    public async Task<List<Responder>> GetRespondersAsync() => await GetAsync<List<Responder>>("api/responders") ?? new();
    public async Task<List<AppUser>> GetUsersAsync() => await GetAsync<List<AppUser>>("api/users") ?? new();

    public Task<bool> AssignResponderAsync(int incidentId, int responderId) =>
        PutAsync($"api/incidents/{incidentId}/assign", new AssignRequest { ResponderId = responderId });

    public Task<bool> UpdateStatusAsync(int incidentId, IncidentStatus status) =>
        PutAsync($"api/incidents/{incidentId}/status", new StatusRequest { Status = status });

    public Task<bool> SetUserActiveAsync(int userId, bool active) =>
        PutAsync($"api/users/{userId}/active", new ActiveRequest { IsActive = active });

    public Task<bool> SaveResponderAsync(Responder r) =>
        r.Id == 0 ? PostAsync("api/responders", r) : PutAsync($"api/responders/{r.Id}", r);

    private async Task<T?> GetAsync<T>(string url)
    {
        ApplyAuth();
        var res = await _http.GetAsync(url);
        if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException();
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<T>(Json);
    }

    private async Task<bool> PutAsync<T>(string url, T body)
    {
        ApplyAuth();
        return (await _http.PutAsJsonAsync(url, body, Json)).IsSuccessStatusCode;
    }

    private async Task<bool> PostAsync<T>(string url, T body)
    {
        ApplyAuth();
        return (await _http.PostAsJsonAsync(url, body, Json)).IsSuccessStatusCode;
    }
}
