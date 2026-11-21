using EmergencyApp.User.ViewModels;

namespace EmergencyApp.User.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _vm;

    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.TryAutoLoginAsync();   // skip login if a session already exists
    }
}
