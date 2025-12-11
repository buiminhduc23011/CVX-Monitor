using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CVX_QLSX.App.Views;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;

namespace CVX_QLSX.App.ViewModels;

/// <summary>
/// Main shell ViewModel for navigation.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private int _selectedMenuIndex;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _currentDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

    private readonly DispatcherTimer _timer;

    private readonly DashboardViewModel _dashboardViewModel;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly ReportViewModel _reportViewModel;

    // Admin credentials
    private const string AdminUsername = "sti";
    private const string AdminPassword = "66668888";

    public MainViewModel(
        DashboardViewModel dashboardViewModel,
        SettingsViewModel settingsViewModel,
        ReportViewModel reportViewModel)
    {
        _dashboardViewModel = dashboardViewModel;
        _settingsViewModel = settingsViewModel;
        _reportViewModel = reportViewModel;

        Title = "STI-CVX Production";
        
        // Default to Dashboard
        CurrentView = _dashboardViewModel;
        SelectedMenuIndex = 0;

        // Start timer for time updates
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => CurrentDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        _timer.Start();
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        CurrentView = _dashboardViewModel;
        SelectedMenuIndex = 0;
    }

    [RelayCommand]
    private async Task NavigateToSettings()
    {
        if (!IsAuthenticated)
        {
            // Show login dialog
            var dialog = new LoginDialog();
            var result = await DialogHost.Show(dialog, "RootDialog");
            
            if (result is true)
            {
                // Validate credentials
                if (dialog.Username == AdminUsername && dialog.Password == AdminPassword)
                {
                    IsAuthenticated = true;
                }
                else
                {
                    // Wrong credentials - show error but don't navigate
                    System.Windows.MessageBox.Show(
                        "Tên đăng nhập hoặc mật khẩu không đúng!", 
                        "Lỗi đăng nhập", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                return; // User cancelled
            }
        }

        CurrentView = _settingsViewModel;
        SelectedMenuIndex = 2;
    }

    [RelayCommand]
    private void NavigateToReport()
    {
        CurrentView = _reportViewModel;
        SelectedMenuIndex = 1;
    }

    [RelayCommand]
    private async Task ShowLogin()
    {
        var dialog = new LoginDialog();
        var result = await DialogHost.Show(dialog, "RootDialog");
        
        if (result is true)
        {
            // Validate credentials
            if (dialog.Username == AdminUsername && dialog.Password == AdminPassword)
            {
                IsAuthenticated = true;
                // Navigate to settings after successful login
                CurrentView = _settingsViewModel;
                SelectedMenuIndex = 2;
            }
            else
            {
                System.Windows.MessageBox.Show(
                    "Tên đăng nhập hoặc mật khẩu không đúng!", 
                    "Lỗi đăng nhập", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
