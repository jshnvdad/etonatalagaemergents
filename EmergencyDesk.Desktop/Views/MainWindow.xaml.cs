using System.Windows;
using EmergencyDesk.ViewModels;

namespace EmergencyDesk.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
