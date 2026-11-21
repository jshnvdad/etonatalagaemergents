using EmergencyApp.User.Services;
using EmergencyApp.User.ViewModels;
using EmergencyApp.User.Views;
using Microsoft.Extensions.Logging;

namespace EmergencyApp.User;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Services
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<LocationService>();
        builder.Services.AddSingleton<SyncService>();

        // ViewModels + Pages
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<HistoryPage>();
        builder.Services.AddTransient<ProfilePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
