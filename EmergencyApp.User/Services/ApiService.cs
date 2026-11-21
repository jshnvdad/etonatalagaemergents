using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EmergencyApp.User.Models;

namespace EmergencyApp.User.Services;

public class ApiService
{
    // 10.0.2.2 = host machine when running in the Android emulator.
    // For a real phone, set your PC's LAN IP (or public URL) in Profile > Server.
    public const string DefaultUrl = "http://10.0.2.2:5000/";

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string BaseUrl
    {
        get => Preferences.Get("server_url", DefaultUrl);
        set => Preferences.Set("server_url", value.EndsWith('/') ? value : value + "/");
    }

    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest r) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", r);

    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest r) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/register", r);

    public Task<ApiResult<EmergencyDto>> CreateEmergencyAsync(CreateEmergencyDto d, string? token) =>
        SendAsync<EmergencyDto>(HttpMethod.Post, "api/emergencies", d, token);

    public Task<ApiResult<List<EmergencyDto>>> GetMyEmergenciesAsync(string? token) =>
        SendAsync<List<EmergencyDto>>(HttpMethod.Get, "api/emergencies/mine", null, token);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body, string? token = null)
    {
        try
        {
            using var req = new HttpRequestMessage(method, new Uri(new Uri(BaseUrl), path));
            if (body is not null) req.Content = JsonContent.Create(body, options: Json);
            if (!string.IsNullOrEmpty(token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var res = await _http.SendAsync(req);
            if (res.IsSuccessStatusCode)
                return new(true, await res.Content.ReadFromJsonAsync<T>(Json), null);

            var msg = await res.Content.ReadAsStringAsync();
            return new(false, default, string.IsNullOrWhiteSpace(msg) ? $"Server error ({(int)res.StatusCode})" : msg);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(false, default, "Cannot reach the server. Check your connection and server URL.", Offline: true);
        }
        catch (Exception ex)
        {
            return new(false, default, ex.Message);
        }
    }
}
