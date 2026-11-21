using EmergencyApp.User.Views;

namespace EmergencyApp.User;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("register", typeof(RegisterPage));
    }
}
