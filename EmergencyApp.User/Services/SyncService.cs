using EmergencyApp.User.Models;

namespace EmergencyApp.User.Services;

public class SyncService
{
    private readonly DatabaseService _db;
    private readonly ApiService _api;
    private readonly SessionService _session;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SyncService(DatabaseService db, ApiService api, SessionService session)
        => (_db, _api, _session) = (db, api, session);

    /// Try to upload one request. Returns true when the server accepted it.
    public async Task<bool> PushAsync(EmergencyRequest r)
    {
        var dto = new CreateEmergencyDto(r.ClientId, r.UserId, r.Type, r.Description,
                                         r.Latitude, r.Longitude, r.CreatedAtUtc);
        var res = await _api.CreateEmergencyAsync(dto, _session.Token);
        if (!res.Success || res.Data is null) return false;

        r.IsSynced = true;
        r.ServerId = res.Data.ServerId;
        r.Status = res.Data.Status;
        await _db.SaveRequestAsync(r);
        return true;
    }

    /// Upload anything still pending, then pull latest statuses from the server.
    public async Task SyncAllAsync()
    {
        if (_session.CurrentUser is null || !await _gate.WaitAsync(0)) return;
        try
        {
            foreach (var r in await _db.GetUnsyncedAsync(_session.CurrentUser.Id))
                await PushAsync(r);

            var mine = await _api.GetMyEmergenciesAsync(_session.Token);
            if (!mine.Success || mine.Data is null) return;

            foreach (var dto in mine.Data)
            {
                var local = await _db.FindByClientIdAsync(dto.ClientId);
                if (local is null) continue;
                local.ServerId = dto.ServerId;
                local.Status = dto.Status;
                local.ResponderName = dto.ResponderName;
                local.IsSynced = true;
                await _db.SaveRequestAsync(local);
            }
        }
        finally { _gate.Release(); }
    }
}
