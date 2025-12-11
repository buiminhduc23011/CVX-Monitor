using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CVX_QLSX.App.Data;
using CVX_QLSX.App.Models;
using CVX_QLSX.App.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;

namespace CVX_QLSX.App.ViewModels;

/// <summary>
/// Dashboard ViewModel for real-time production monitoring with plan queue.
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly ITcpService _tcpService;
    private readonly ISettingsService _settingsService;
    private readonly CounterService _counterService;
    private readonly ProductionContext _dbContext;

    // Product list loaded from settings
    public ObservableCollection<string> ProductList { get; } = new();

    public ObservableCollection<string> FilteredProducts { get; } = new();

    // Current production data
    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _okCount;

    [ObservableProperty]
    private int _ngCount;

    [ObservableProperty]
    private string _productId = "-";

    [ObservableProperty]
    private string _mfgDateDisplay = "-";

    [ObservableProperty]
    private string _expDateDisplay = "-";

    // Connection status
    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionStatus = "Chưa kết nối";

    // Current Plan
    [ObservableProperty]
    private ProductionPlan? _currentPlan;

    partial void OnCurrentPlanChanged(ProductionPlan? oldValue, ProductionPlan? newValue)
    {
        // Unsubscribe from old plan
        if (oldValue != null)
            oldValue.PropertyChanged -= OnCurrentPlanPropertyChanged;
        
        // Subscribe to new plan
        if (newValue != null)
            newValue.PropertyChanged += OnCurrentPlanPropertyChanged;
    }

    private void OnCurrentPlanPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductionPlan.TargetQuantity))
        {
            // Update ViewModel's TargetQuantity and chart when plan's TargetQuantity changes
            Application.Current?.Dispatcher.Invoke(() =>
            {
                UpdateCurrentPlanStatus();
                UpdateDoughnutChart();
            });
        }
    }

    [ObservableProperty]
    private bool _hasCurrentPlan;

    [ObservableProperty]
    private string _currentPlanStatusText = "";

    [ObservableProperty]
    private int _targetQuantity;

    [ObservableProperty]
    private double _yieldRate;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _progressText = "0%";

    // All Plans (for history display)
    public ObservableCollection<ProductionPlan> AllPlans { get; } = new();

    // New Plan Form
    [ObservableProperty]
    private string _newPlanProduct = string.Empty;

    [ObservableProperty]
    private string? _selectedProduct;

    [ObservableProperty]
    private int _newPlanQuantity = 1000;

    [RelayCommand]
    private void IncrementQuantity() => NewPlanQuantity += 100;

    [RelayCommand]
    private void DecrementQuantity() => NewPlanQuantity = Math.Max(1, NewPlanQuantity - 100);

    partial void OnSelectedProductChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            NewPlanProduct = value;
        }
    }

    // Doughnut Chart
    public IEnumerable<ISeries> DoughnutSeries { get; set; } = Array.Empty<ISeries>();

    public DashboardViewModel(ITcpService tcpService, ISettingsService settingsService, 
        CounterService counterService, ProductionContext dbContext)
    {
        _tcpService = tcpService;
        _settingsService = settingsService;
        _counterService = counterService;
        _dbContext = dbContext;

        Title = "Dashboard";

        // Load products from settings
        var settings = _settingsService.Load();
        foreach (var product in settings.ProductList)
        {
            ProductList.Add(product);
            FilteredProducts.Add(product);
        }

        _tcpService.DataReceived += OnDataReceived;
        _tcpService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _counterService.CountsUpdated += OnCountsUpdated;

        _ = LoadPlansAsync();
        UpdateDoughnutChart();
        _ = AutoConnectAsync();
    }

    partial void OnNewPlanProductChanged(string value)
    {
        FilteredProducts.Clear();
        var searchText = value?.ToLower() ?? "";
        foreach (var product in ProductList)
        {
            if (string.IsNullOrEmpty(searchText) || product.ToLower().Contains(searchText))
                FilteredProducts.Add(product);
        }
    }

    private async Task LoadPlansAsync()
    {
        AllPlans.Clear();

        // Load ALL plans for history display (ordered by most recent first)
        var allPlans = await _dbContext.ProductionPlans
            .OrderByDescending(p => p.CreatedAt)
            .Take(50) // Limit to last 50 plans
            .ToListAsync();

        foreach (var plan in allPlans)
            AllPlans.Add(plan);

        // Find or set current running plan
        CurrentPlan = AllPlans.FirstOrDefault(p => p.Status == PlanStatus.Running);
        
        if (CurrentPlan == null)
        {
            // Check for waiting plan
            var waitingPlan = AllPlans.FirstOrDefault(p => p.Status == PlanStatus.Waiting);
            if (waitingPlan != null)
            {
                waitingPlan.Status = PlanStatus.Running;
                waitingPlan.StartedAt = DateTime.Now;
                CurrentPlan = waitingPlan;
                await _dbContext.SaveChangesAsync();
            }
        }

        // Restore counter state from current plan
        if (CurrentPlan != null)
        {
            _counterService.RestoreState(CurrentPlan.CurrentQuantity, CurrentPlan.CurrentQuantity, 0);
            TotalCount = CurrentPlan.CurrentQuantity;
            OkCount = CurrentPlan.CurrentQuantity; // Assuming OK = Total for simplicity
            NgCount = 0;
        }

        UpdateCurrentPlanStatus();
        UpdateDoughnutChart();
    }

    private void UpdateCurrentPlanStatus()
    {
        HasCurrentPlan = CurrentPlan != null;
        
        if (CurrentPlan != null)
        {
            CurrentPlanStatusText = $"Đang SX: {CurrentPlan.ProductName}";
            TargetQuantity = CurrentPlan.TargetQuantity;
        }
        else
        {
            CurrentPlanStatusText = "";
            TargetQuantity = 0;
        }
    }

    private void UpdateYieldRate()
    {
        YieldRate = TotalCount > 0 ? (double)OkCount / TotalCount * 100 : 0;
    }

    private void UpdateDoughnutChart()
    {
        int current = CurrentPlan?.CurrentQuantity ?? 0;
        int target = CurrentPlan?.TargetQuantity ?? 1000;
        int remaining = Math.Max(0, target - current);

        ProgressPercentage = target > 0 ? (double)current / target * 100 : 0;
        ProgressText = $"{ProgressPercentage:F0}%";

        DoughnutSeries = new ISeries[]
        {
            new PieSeries<int>
            {
                Values = new[] { current },
                Name = "Đã sản xuất",
                Fill = new SolidColorPaint(new SKColor(33, 150, 243)),
                InnerRadius = 70,
                MaxRadialColumnWidth = 40
            },
            new PieSeries<int>
            {
                Values = new[] { remaining },
                Name = "Còn lại",
                Fill = new SolidColorPaint(new SKColor(224, 224, 224)),
                InnerRadius = 70,
                MaxRadialColumnWidth = 40
            }
        };

        OnPropertyChanged(nameof(DoughnutSeries));
    }

    private async Task AutoConnectAsync()
    {
        var settings = _settingsService.Load();
        if (!string.IsNullOrEmpty(settings.CameraIpAddress) && settings.AutoReconnect)
        {
            try
            {
                await _tcpService.ConnectAsync(settings.CameraIpAddress, settings.CameraPort);
                await _tcpService.StartReadingAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Auto-connect failed: {ex.Message}");
            }
        }
    }

    private void OnDataReceived(object? sender, ProductionData data)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (!HasCurrentPlan)
            {
                System.Diagnostics.Debug.WriteLine("[Dashboard] No plan - data not recorded");
                return;
            }

            var packet = new CameraPacket
            {
                TotalCount = data.TotalCount,
                OKCount = data.OKCount,
                NGCount = data.NGCount,
                ProductId = data.ProductId,
                RawMfgDate = data.MfgDate,
                RawExpDate = data.ExpDate
            };

            _counterService.ProcessPacket(packet);

            ProductId = data.ProductId;
            MfgDateDisplay = FormatDate(data.MfgDate);
            ExpDateDisplay = FormatDate(data.ExpDate);
        });
    }

    private static string FormatDate(string rawDate)
    {
        if (string.IsNullOrWhiteSpace(rawDate) || rawDate.Length != 6)
            return rawDate;

        if (DateTime.TryParseExact(rawDate, "ddMMyy", CultureInfo.InvariantCulture, 
            DateTimeStyles.None, out DateTime date))
            return date.ToString("dd/MM/yyyy");
        return rawDate;
    }

    private void OnCountsUpdated(object? sender, CounterUpdatedEventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            TotalCount = e.Total;
            OkCount = e.OK;
            NgCount = e.NG;
            UpdateYieldRate();

            System.Diagnostics.Debug.WriteLine($"[Dashboard] OnCountsUpdated: Total={e.Total}, CurrentPlan={(CurrentPlan != null ? CurrentPlan.ProductName : "NULL")}");

            if (CurrentPlan != null)
            {
                CurrentPlan.CurrentQuantity = e.Total;
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Updated CurrentPlan.CurrentQuantity to {CurrentPlan.CurrentQuantity}");
                
                // Save to database periodically (every update)
                _ = SavePlanProgressAsync();

                if (CurrentPlan.CurrentQuantity >= CurrentPlan.TargetQuantity)
                    _ = CompletePlanAndMoveNextAsync();
            }

            UpdateDoughnutChart();
        });
    }

    private async Task SavePlanProgressAsync()
    {
        if (CurrentPlan == null) return;
        
        try
        {
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Saving CurrentQuantity={CurrentPlan.CurrentQuantity} for plan {CurrentPlan.Id}");
            // Explicitly mark the entity as modified so EF Core tracks the change
            _dbContext.Entry(CurrentPlan).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            await _dbContext.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Saved successfully!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Save error: {ex.Message}");
        }
    }

    private async Task CompletePlanAndMoveNextAsync()
    {
        if (CurrentPlan == null) return;

        // Store completed plan reference before changing CurrentPlan
        var completedPlan = CurrentPlan;
        
        completedPlan.Status = PlanStatus.Completed;
        completedPlan.CompletedAt = DateTime.Now;

        // Save to production records for reporting
        var record = new ProductionRecord
        {
            Timestamp = DateTime.Now,
            ProductId = completedPlan.ProductName,
            Total = completedPlan.CurrentQuantity,
            OK = OkCount,
            NG = NgCount,
            MfgDate = MfgDateDisplay,
            ExpDate = ExpDateDisplay
        };
        _dbContext.ProductionRecords.Add(record);
        
        // Save the completed plan with its final CurrentQuantity BEFORE resetting counter
        _dbContext.Entry(completedPlan).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        await _dbContext.SaveChangesAsync();

        // Find next waiting plan BEFORE resetting counter
        // This prevents OnCountsUpdated from overwriting completedPlan.CurrentQuantity
        CurrentPlan = AllPlans.FirstOrDefault(p => p.Status == PlanStatus.Waiting);
        if (CurrentPlan != null)
        {
            CurrentPlan.Status = PlanStatus.Running;
            CurrentPlan.StartedAt = DateTime.Now;
            await _dbContext.SaveChangesAsync();
        }

        // Now reset counter - OnCountsUpdated will update the NEW CurrentPlan (or do nothing if null)
        _counterService.ResetForNewShift();

        UpdateCurrentPlanStatus();
        UpdateDoughnutChart();
    }

    private void OnConnectionStatusChanged(object? sender, bool isConnected)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            IsConnected = isConnected;
            ConnectionStatus = isConnected ? "Đã kết nối" : "Chưa kết nối";
        });
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        try
        {
            IsBusy = true;
            var settings = _settingsService.Load();
            await _tcpService.ConnectAsync(settings.CameraIpAddress, settings.CameraPort);
            await _tcpService.StartReadingAsync();
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _tcpService.DisconnectAsync();
    }

    [RelayCommand]
    private async Task AddPlanAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPlanProduct) || NewPlanQuantity <= 0)
            return;

        int maxOrder = AllPlans.Any() ? AllPlans.Max(p => p.QueueOrder) : 0;

        var plan = new ProductionPlan
        {
            ProductName = NewPlanProduct,
            TargetQuantity = NewPlanQuantity,
            QueueOrder = maxOrder + 1,
            Status = PlanStatus.Waiting
        };

        _dbContext.ProductionPlans.Add(plan);
        await _dbContext.SaveChangesAsync();
        AllPlans.Insert(0, plan); // Add to top of list

        if (CurrentPlan == null)
        {
            CurrentPlan = plan;
            CurrentPlan.Status = PlanStatus.Running;
            CurrentPlan.StartedAt = DateTime.Now;
            await _dbContext.SaveChangesAsync();
            UpdateCurrentPlanStatus();
            UpdateDoughnutChart();
        }

        NewPlanProduct = string.Empty;
        NewPlanQuantity = 1000;
    }

    [RelayCommand]
    private async Task StopPlanAsync(ProductionPlan plan)
    {
        if (plan == null) return;
        
        // Skip if already completed or cancelled
        if (plan.Status == PlanStatus.Completed || plan.Status == PlanStatus.Cancelled)
            return;

        bool wasCurrentPlan = (plan == CurrentPlan);

        if (plan.Status == PlanStatus.Running)
        {
            // Running → Complete (manual completion, save record)
            var record = new ProductionRecord
            {
                Timestamp = DateTime.Now,
                ProductId = plan.ProductName,
                Total = plan.CurrentQuantity,
                OK = OkCount,
                NG = NgCount,
                MfgDate = MfgDateDisplay,
                ExpDate = ExpDateDisplay
            };
            _dbContext.ProductionRecords.Add(record);

            plan.Status = PlanStatus.Completed;
            plan.CompletedAt = DateTime.Now;
        }
        else // Waiting → Cancel
        {
            plan.Status = PlanStatus.Cancelled;
            plan.CancelledAt = DateTime.Now;
        }

        await _dbContext.SaveChangesAsync();

        if (wasCurrentPlan)
        {
            // Change CurrentPlan BEFORE resetting counter
            // This prevents OnCountsUpdated from overwriting the completed plan's CurrentQuantity
            CurrentPlan = AllPlans.FirstOrDefault(p => p.Status == PlanStatus.Waiting);
            if (CurrentPlan != null)
            {
                CurrentPlan.Status = PlanStatus.Running;
                CurrentPlan.StartedAt = DateTime.Now;
                await _dbContext.SaveChangesAsync();
            }
            
            // Now reset counter - OnCountsUpdated will only affect the NEW CurrentPlan (or nothing if null)
            _counterService.ResetForNewShift();
            
            UpdateCurrentPlanStatus();
            UpdateDoughnutChart();
        }
    }
}
