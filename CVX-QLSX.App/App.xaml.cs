using System.Windows;
using CVX_QLSX.App.Data;
using CVX_QLSX.App.Services;
using CVX_QLSX.App.ViewModels;

namespace CVX_QLSX.App;

public partial class App : Application
{
    // Simple service locator for this demo
    public static ProductionContext DbContext { get; private set; } = null!;
    public static ITcpService TcpService { get; private set; } = null!;
    public static ISettingsService SettingsService { get; private set; } = null!;
    public static CounterService CounterService { get; private set; } = null!;
    public static MainViewModel MainViewModel { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize services
        DbContext = new ProductionContext();
        DbContext.Initialize();

        SettingsService = new SettingsService();
        TcpService = new TcpService();
        CounterService = new CounterService();

        // Initialize ViewModels
        var dashboardVm = new DashboardViewModel(TcpService, SettingsService, CounterService, DbContext);
        var settingsVm = new SettingsViewModel(SettingsService, TcpService);
        var reportVm = new ReportViewModel(DbContext);

        MainViewModel = new MainViewModel(dashboardVm, settingsVm, reportVm);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Cleanup
        (TcpService as IDisposable)?.Dispose();
        DbContext?.Dispose();

        base.OnExit(e);
    }
}
