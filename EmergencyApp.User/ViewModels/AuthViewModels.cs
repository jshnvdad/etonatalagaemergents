using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmergencyApp.User.Models;
using EmergencyApp.User.Services;

namespace EmergencyApp.User.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly SessionService _session;

    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string error = "";
    [ObservableProperty] private bool isBusy;

    public LoginViewModel(ApiService api, SessionService session) => (_api, _session) = (api, session);

    public async Task TryAutoLoginAsync()
    {
        if (await _session.RestoreAsync())
            await Shell.Current.GoToAsync("//main");
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        { Error = "Enter your email and password."; return; }

        IsBusy = true; Error = "";
        try
        {
            var res = await _api.LoginAsync(new LoginRequest(Email.Trim(), Password));
            if (!res.Success || res.Data is null) { Error = res.Error ?? "Login failed."; return; }
            await _session.SignInAsync(res.Data);
            Password = "";
            await Shell.Current.GoToAsync("//main");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand] private Task GoToRegisterAsync() => Shell.Current.GoToAsync("register");

    [RelayCommand]
    private async Task ServerSettingsAsync()
    {
        var url = await Shell.Current.DisplayPromptAsync("Server URL", "Address of the emergency server",
            initialValue: ApiService.BaseUrl, keyboard: Keyboard.Url);
        if (!string.IsNullOrWhiteSpace(url)) ApiService.BaseUrl = url.Trim();
    }
}

public partial class RegisterViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly SessionService _session;

    [ObservableProperty] private string fullName = "";
    [ObservableProperty] private string phone = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string error = "";
    [ObservableProperty] private bool isBusy;

    public RegisterViewModel(ApiService api, SessionService session) => (_api, _session) = (api, session);

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Phone) ||
            string.IsNullOrWhiteSpace(Email) || Password.Length < 6)
        { Error = "Fill in all fields. Password needs at least 6 characters."; return; }

        IsBusy = true; Error = "";
        try
        {
            var res = await _api.RegisterAsync(new RegisterRequest(FullName.Trim(), Phone.Trim(), Email.Trim(), Password));
            if (!res.Success || res.Data is null) { Error = res.Error ?? "Registration failed."; return; }
            await _session.SignInAsync(res.Data);
            await Shell.Current.GoToAsync("//main");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand] private Task BackAsync() => Shell.Current.GoToAsync("..");
}
