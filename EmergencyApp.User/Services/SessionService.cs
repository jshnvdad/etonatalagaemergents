using EmergencyApp.User.Models;

namespace EmergencyApp.User.Services;

public class SessionService
{
    private readonly DatabaseService _db;
    public SessionService(DatabaseService db) => _db = db;

    public LocalUser? CurrentUser { get; private set; }
    public string? Token { get; private set; }
    public bool IsLoggedIn => CurrentUser is not null;

    public async Task<bool> RestoreAsync()
    {
        var id = Preferences.Get("user_id", 0);
        if (id == 0) return false;
        Token = await SecureStorage.Default.GetAsync("token");
        CurrentUser = await _db.GetUserAsync(id);
        return CurrentUser is not null;
    }

    public async Task SignInAsync(AuthResponse a)
    {
        var existing = await _db.GetUserAsync(a.UserId);
        var user = existing ?? new LocalUser { Id = a.UserId };
        user.FullName = a.FullName; user.Phone = a.Phone; user.Email = a.Email;
        await _db.SaveUserAsync(user);

        CurrentUser = user;
        Token = a.Token;
        Preferences.Set("user_id", a.UserId);
        await SecureStorage.Default.SetAsync("token", a.Token);
    }

    public void SignOut()
    {
        CurrentUser = null; Token = null;
        Preferences.Remove("user_id");
        SecureStorage.Default.Remove("token");
    }
}
