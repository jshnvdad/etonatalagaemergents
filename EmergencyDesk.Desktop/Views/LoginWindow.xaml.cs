using System.Windows;
using System.Windows.Controls;
using EmergencyDesk.Services;

namespace EmergencyDesk.Views;

public partial class LoginWindow : Window
{
    public LoginWindow() => InitializeComponent();

    private async void SignIn_Click(object sender, RoutedEventArgs e) => await SignInAsync();

    private async void Field_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter) await SignInAsync();
    }

    private async Task SignInAsync()
    {
        ErrorText.Text = "";
        if (string.IsNullOrWhiteSpace(UserBox.Text) || string.IsNullOrEmpty(PassBox.Password))
        {
            ErrorText.Text = "Enter your username and password.";
            return;
        }

        SignInButton.IsEnabled = false;
        try
        {
            var result = await App.Api.LoginAsync(UserBox.Text.Trim(), PassBox.Password);
            if (result is null)
            {
                ErrorText.Text = "Invalid username or password.";
                return;
            }
            App.Session.Token = result.Token;
            App.Session.DisplayName = result.DisplayName;
            App.Session.Role = result.Role;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Cannot reach server: {ex.Message}";
        }
        finally
        {
            SignInButton.IsEnabled = true;
        }
    }
}
