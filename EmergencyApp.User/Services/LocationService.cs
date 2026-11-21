namespace EmergencyApp.User.Services;

public class LocationService
{
    /// Returns null if permission is denied or no fix is available. Call from the UI thread.
    public async Task<Location?> GetCurrentAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted) return null;

        try
        {
            var fresh = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));
            return fresh ?? await Geolocation.Default.GetLastKnownLocationAsync();
        }
        catch
        {
            try { return await Geolocation.Default.GetLastKnownLocationAsync(); } catch { return null; }
        }
    }
}
