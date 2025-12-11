using CommunityToolkit.Mvvm.ComponentModel;

namespace CVX_QLSX.App.ViewModels;

/// <summary>
/// Base ViewModel class with common functionality.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;
}
