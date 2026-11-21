using System.Windows;
using EmergencyDesk.Services;
using EmergencyDesk.Views;

namespace EmergencyDesk;

public partial class App : Application
{
    public static AppSettings Settings { get; } = AppSettings.Load();
    public static SessionService Session { get; } = new();
    public static ApiClient Api { get; } = new(Settings, Session);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        ShowLogin();
    }

    public static void ShowLogin()
    {
        Session.Clear();
        var login = new LoginWindow();
        if (login.ShowDialog() == true)
        {
            var main = new MainWindow();
            Current.MainWindow = main;
            main.Show();
        }
        else
        {
            Current.Shutdown();
        }
    }
}
