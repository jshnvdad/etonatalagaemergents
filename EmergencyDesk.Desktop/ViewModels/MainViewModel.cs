using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Media;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmergencyDesk.Models;

namespace EmergencyDesk.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DispatcherTimer _timer;
    private int _maxSeenIncidentId;
    private bool _firstLoad = true;
    private bool _refreshing;

    public MainViewModel()
    {
        Incidents = new ObservableCollection<Incident>();
        Responders = new ObservableCollection<Responder>();
        Users = new ObservableCollection<AppUser>();

        IncidentsView = CollectionViewSource.GetDefaultView(Incidents);
        IncidentsView.Filter = o => o is Incident i &&
            (StatusFilter == "All" || i.Status.ToString() == StatusFilter);
        IncidentsView.SortDescriptions.Add(new SortDescription(nameof(Incident.ReportedAt), ListSortDirection.Descending));

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Max(2, App.Settings.RefreshSeconds)) };
        _timer.Tick += async (_, _) => await RefreshAsync();
        _timer.Start();
        _ = RefreshAsync();
    }

    // ---- session ----
    public string DisplayName => App.Session.DisplayName;
    public string Role => App.Session.Role;
    public bool IsAdmin => App.Session.IsAdmin;

    // ---- data ----
    public ObservableCollection<Incident> Incidents { get; }
    public ObservableCollection<Responder> Responders { get; }
    public ObservableCollection<AppUser> Users { get; }
    public ICollectionView IncidentsView { get; }

    public IncidentStatus[] IncidentStatuses { get; } = Enum.GetValues<IncidentStatus>();
    public IncidentType[] IncidentTypes { get; } = Enum.GetValues<IncidentType>();
    public ResponderStatus[] ResponderStatuses { get; } = Enum.GetValues<ResponderStatus>();
    public string[] StatusFilters { get; } =
        new[] { "All" }.Concat(Enum.GetNames<IncidentStatus>()).ToArray();

    // ---- navigation ----
    [ObservableProperty] private string _currentPage = "Dashboard";
    [RelayCommand] private void Navigate(string page) => CurrentPage = page;

    // ---- status ----
    [ObservableProperty] private string _statusMessage = "Connecting...";
    [ObservableProperty] private bool _isOnline;
    [ObservableProperty] private DateTime? _lastUpdated;

    // ---- dashboard counters ----
    [ObservableProperty] private int _pendingCount;
    [ObservableProperty] private int _activeCount;
    [ObservableProperty] private int _resolvedCount;
    [ObservableProperty] private int _availableResponders;
    [ObservableProperty] private int _totalUsers;

    // ---- incidents ----
    [ObservableProperty] private string _statusFilter = "All";
    partial void OnStatusFilterChanged(string value) => IncidentsView.Refresh();

    [ObservableProperty] private Incident? _selectedIncident;
    [ObservableProperty] private Responder? _assignChoice;
    [ObservableProperty] private IncidentStatus _newStatusChoice = IncidentStatus.Dispatched;

    [RelayCommand]
    private async Task AssignAsync()
    {
        if (SelectedIncident is null || AssignChoice is null) return;
        var id = SelectedIncident.Id;
        var ok = await App.Api.AssignResponderAsync(id, AssignChoice.Id);
        StatusMessage = ok ? $"Assigned {AssignChoice.Name} to incident #{id}." : "Assignment failed.";
        await RefreshAsync();
        SelectedIncident = Incidents.FirstOrDefault(i => i.Id == id);
    }

    [RelayCommand]
    private async Task UpdateStatusAsync()
    {
        if (SelectedIncident is null) return;
        var id = SelectedIncident.Id;
        var ok = await App.Api.UpdateStatusAsync(id, NewStatusChoice);
        StatusMessage = ok ? $"Incident #{id} set to {NewStatusChoice}." : "Status update failed.";
        await RefreshAsync();
        SelectedIncident = Incidents.FirstOrDefault(i => i.Id == id);
    }

    // ---- responders ----
    [ObservableProperty] private Responder? _selectedResponder;
    [ObservableProperty] private string _editName = "";
    [ObservableProperty] private string _editUnit = "";
    [ObservableProperty] private string _editPhone = "";
    [ObservableProperty] private IncidentType _editSpecialty = IncidentType.Medical;
    [ObservableProperty] private ResponderStatus _editStatus = ResponderStatus.Available;

    partial void OnSelectedResponderChanged(Responder? value)
    {
        if (value is null) return;
        EditName = value.Name; EditUnit = value.Unit; EditPhone = value.Phone;
        EditSpecialty = value.Specialty; EditStatus = value.Status;
    }

    [RelayCommand]
    private void NewResponder()
    {
        SelectedResponder = null;
        EditName = EditUnit = EditPhone = "";
        EditSpecialty = IncidentType.Medical;
        EditStatus = ResponderStatus.Available;
    }

    [RelayCommand]
    private async Task SaveResponderAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName) || string.IsNullOrWhiteSpace(EditUnit))
        {
            StatusMessage = "Responder name and unit are required.";
            return;
        }
        var r = new Responder
        {
            Id = SelectedResponder?.Id ?? 0,
            Name = EditName.Trim(), Unit = EditUnit.Trim(), Phone = EditPhone.Trim(),
            Specialty = EditSpecialty, Status = EditStatus
        };
        StatusMessage = await App.Api.SaveResponderAsync(r) ? "Responder saved." : "Saving responder failed.";
        await RefreshAsync();
    }

    // ---- users (admin only) ----
    [ObservableProperty] private AppUser? _selectedUser;

    [RelayCommand]
    private async Task ToggleUserAsync()
    {
        if (!IsAdmin || SelectedUser is null) return;
        var target = !SelectedUser.IsActive;
        var name = SelectedUser.FullName;
        var ok = await App.Api.SetUserActiveAsync(SelectedUser.Id, target);
        StatusMessage = ok ? $"{name} {(target ? "activated" : "deactivated")}." : "User update failed.";
        await RefreshAsync();
    }

    // ---- refresh / logout ----
    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (_refreshing) return;
        _refreshing = true;
        try
        {
            var incidents = await App.Api.GetIncidentsAsync();
            var responders = await App.Api.GetRespondersAsync();
            var users = IsAdmin ? await App.Api.GetUsersAsync() : new List<AppUser>();

            var selIncident = SelectedIncident?.Id;
            var selResponder = SelectedResponder?.Id;
            var selUser = SelectedUser?.Id;

            Replace(Incidents, incidents);
            Replace(Responders, responders);
            Replace(Users, users);

            if (selIncident is not null) SelectedIncident = Incidents.FirstOrDefault(i => i.Id == selIncident);
            if (selResponder is not null) SelectedResponder = Responders.FirstOrDefault(r => r.Id == selResponder);
            if (selUser is not null) SelectedUser = Users.FirstOrDefault(u => u.Id == selUser);

            PendingCount = incidents.Count(i => i.Status == IncidentStatus.Received);
            ActiveCount = incidents.Count(i => i.Status is IncidentStatus.Dispatched or IncidentStatus.EnRoute or IncidentStatus.OnScene);
            ResolvedCount = incidents.Count(i => i.Status == IncidentStatus.Resolved);
            AvailableResponders = responders.Count(r => r.Status == ResponderStatus.Available);
            TotalUsers = users.Count;

            var newest = incidents.Count == 0 ? 0 : incidents.Max(i => i.Id);
            if (!_firstLoad && newest > _maxSeenIncidentId)
            {
                SystemSounds.Exclamation.Play();
                StatusMessage = "New emergency report received!";
            }
            _maxSeenIncidentId = Math.Max(_maxSeenIncidentId, newest);
            _firstLoad = false;

            IsOnline = true;
            LastUpdated = DateTime.Now;
            if (StatusMessage is "Connecting..." or "Server unreachable.") StatusMessage = "Connected.";
        }
        catch (UnauthorizedAccessException)
        {
            _timer.Stop();
            MessageBox.Show("Your session has expired. Please sign in again.", "Session expired");
            Logout();
        }
        catch (Exception)
        {
            IsOnline = false;
            StatusMessage = "Server unreachable.";
        }
        finally
        {
            _refreshing = false;
        }
    }

    [RelayCommand]
    private void Logout()
    {
        _timer.Stop();
        var old = Application.Current.MainWindow;
        App.ShowLogin();
        old?.Close();
    }

    private static void Replace<T>(ObservableCollection<T> target, List<T> source)
    {
        target.Clear();
        foreach (var item in source) target.Add(item);
    }
}
