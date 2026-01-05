using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CVX_QLSX.App.Data;
using CVX_QLSX.App.Models;
using CVX_QLSX.App.Services;

namespace CVX_QLSX.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ITcpService _tcpService;

    // Camera settings
    [ObservableProperty]
    private string _cameraIpAddress = "192.168.1.100";

    [ObservableProperty]
    private int _cameraPort = 8500;

    [ObservableProperty]
    private bool _autoReconnect = true;

    [ObservableProperty]
    private string _testConnectionResult = string.Empty;

    // Product settings
    public ObservableCollection<string> ProductList { get; } = new();

    [ObservableProperty]
    private string _newProduct = string.Empty;

    [ObservableProperty]
    private string? _selectedProduct;

    public SettingsViewModel(ISettingsService settingsService, ITcpService tcpService)
    {
        _settingsService = settingsService;
        _tcpService = tcpService;

        Title = "Cài đặt";

        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Load();
        CameraIpAddress = settings.CameraIpAddress;
        CameraPort = settings.CameraPort;
        AutoReconnect = settings.AutoReconnect;

        ProductList.Clear();
        foreach (var product in settings.ProductList)
            ProductList.Add(product);
    }

    [RelayCommand]
    private void SaveSettings()
    {
        var currentSettings = _settingsService.Load();
        currentSettings.CameraIpAddress = CameraIpAddress;
        currentSettings.CameraPort = CameraPort;
        currentSettings.AutoReconnect = AutoReconnect;
        currentSettings.ProductList = ProductList.ToList();
        
        _settingsService.Save(currentSettings);
        TestConnectionResult = "Đã lưu cài đặt!";
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            IsBusy = true;
            TestConnectionResult = "Đang kết nối...";

            await _tcpService.ConnectAsync(CameraIpAddress, CameraPort);
            await _tcpService.DisconnectAsync();

            TestConnectionResult = "✓ Kết nối thành công!";
        }
        catch (Exception ex)
        {
            TestConnectionResult = $"✗ Lỗi: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddProduct()
    {
        if (string.IsNullOrWhiteSpace(NewProduct))
            return;

        if (!ProductList.Contains(NewProduct))
        {
            ProductList.Add(NewProduct);
            SaveSettings();
        }
        NewProduct = string.Empty;
    }

    [RelayCommand]
    private void RemoveProduct()
    {
        if (SelectedProduct == null) return;

        ProductList.Remove(SelectedProduct);
        SaveSettings();
        SelectedProduct = null;
    }
}
