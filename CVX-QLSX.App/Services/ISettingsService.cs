using CVX_QLSX.App.Models;

namespace CVX_QLSX.App.Services;

/// <summary>
/// Interface for application settings management.
/// </summary>
public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}
