using EmergencyApp.User.Models;
using SQLite;

namespace EmergencyApp.User.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private async Task<SQLiteAsyncConnection> Db()
    {
        if (_db is not null) return _db;
        await _lock.WaitAsync();
        try
        {
            if (_db is null)
            {
                var path = Path.Combine(FileSystem.AppDataDirectory, "emergency_user.db3");
                var conn = new SQLiteAsyncConnection(path,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
                await conn.CreateTableAsync<LocalUser>();
                await conn.CreateTableAsync<EmergencyContact>();
                await conn.CreateTableAsync<EmergencyRequest>();
                _db = conn;
            }
        }
        finally { _lock.Release(); }
        return _db;
    }

    // Users
    public async Task SaveUserAsync(LocalUser u) => await (await Db()).InsertOrReplaceAsync(u);
    public async Task<LocalUser?> GetUserAsync(int id) =>
        await (await Db()).Table<LocalUser>().Where(x => x.Id == id).FirstOrDefaultAsync();

    // Contacts
    public async Task<List<EmergencyContact>> GetContactsAsync(int userId) =>
        await (await Db()).Table<EmergencyContact>().Where(c => c.UserId == userId).ToListAsync();
    public async Task SaveContactAsync(EmergencyContact c) => await (await Db()).InsertAsync(c);
    public async Task DeleteContactAsync(EmergencyContact c) => await (await Db()).DeleteAsync(c);

    // Requests
    public async Task SaveRequestAsync(EmergencyRequest r)
    {
        var db = await Db();
        if (r.LocalId == 0) await db.InsertAsync(r); else await db.UpdateAsync(r);
    }
    public async Task<List<EmergencyRequest>> GetRequestsAsync(int userId) =>
        await (await Db()).Table<EmergencyRequest>().Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAtUtc).ToListAsync();
    public async Task<List<EmergencyRequest>> GetUnsyncedAsync(int userId) =>
        await (await Db()).Table<EmergencyRequest>().Where(r => r.UserId == userId && !r.IsSynced).ToListAsync();
    public async Task<EmergencyRequest?> FindByClientIdAsync(string clientId) =>
        await (await Db()).Table<EmergencyRequest>().Where(r => r.ClientId == clientId).FirstOrDefaultAsync();
}
