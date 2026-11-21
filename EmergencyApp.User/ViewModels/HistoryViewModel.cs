using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmergencyApp.User.Models;
using EmergencyApp.User.Services;

namespace EmergencyApp.User.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly SessionService _session;
    private readonly SyncService _sync;

    public ObservableCollection<EmergencyRequest> Items { get; } = new();
    [ObservableProperty] private bool isRefreshing;

    public HistoryViewModel(DatabaseService db, SessionService session, SyncService sync)
        => (_db, _session, _sync) = (db, session, sync);

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (_session.CurrentUser is null) { IsRefreshing = false; return; }
        IsRefreshing = true;
        try
        {
            await Reload();             // show cached data immediately
            await _sync.SyncAllAsync(); // push pending + pull statuses
            await Reload();
        }
        finally { IsRefreshing = false; }
    }

    private async Task Reload()
    {
        var list = await _db.GetRequestsAsync(_session.CurrentUser!.Id);
        Items.Clear();
        foreach (var r in list) Items.Add(r);
    }
}
