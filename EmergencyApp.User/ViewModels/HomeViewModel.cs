using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmergencyApp.User.Models;
using EmergencyApp.User.Services;

namespace EmergencyApp.User.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly SessionService _session;
    private readonly LocationService _location;
    private readonly SyncService _sync;

    public List<EmergencyTypeOption> Types => EmergencyTypeOption.All;

    [ObservableProperty] private EmergencyTypeOption? selectedType;
    [ObservableProperty] private string description = "";
    [ObservableProperty] private string statusText = "";
    [ObservableProperty] private string greeting = "";
    [ObservableProperty] private bool isBusy;

    public HomeViewModel(DatabaseService db, SessionService session, LocationService location, SyncService sync)
        => (_db, _session, _location, _sync) = (db, session, location, sync);

    public void OnAppearing() => Greeting = $"Hello, {_session.CurrentUser?.FullName.Split(' ')[0]}";

    [RelayCommand]
    private async Task SendSosAsync()
    {
        if (IsBusy || _session.CurrentUser is null) return;
        if (SelectedType is null)
        {
            await Shell.Current.DisplayAlert("Choose type", "Select the type of emergency first.", "OK");
            return;
        }
        if (!await Shell.Current.DisplayAlert("Send SOS",
                $"Send a {SelectedType.Name} emergency with your current location?", "SEND", "Cancel"))
            return;

        IsBusy = true; StatusText = "Getting your location…";
        try
        {
            var loc = await _location.GetCurrentAsync();
            var req = new EmergencyRequest
            {
                UserId = _session.CurrentUser.Id,
                Type = SelectedType.Name,
                Description = Description.Trim(),
                Latitude = loc?.Latitude,
                Longitude = loc?.Longitude,
            };
            await _db.SaveRequestAsync(req);   // always saved locally first (offline-safe)

            StatusText = "Sending…";
            var sent = await _sync.PushAsync(req);
            StatusText = sent
                ? "✅ Help request sent. Track it in History."
                : "⚠️ No connection. Saved on your phone and will send when you open History / reconnect. Call the emergency number now if urgent.";
            if (loc is null) StatusText += "\n📍 Location was unavailable.";

            Description = ""; SelectedType = null;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CallEmergencyAsync()
    {
        var number = Preferences.Get("emergency_number", "911");
        try { PhoneDialer.Default.Open(number); }
        catch { await Shell.Current.DisplayAlert("Error", "Cannot open the phone dialer.", "OK"); }
    }
}
