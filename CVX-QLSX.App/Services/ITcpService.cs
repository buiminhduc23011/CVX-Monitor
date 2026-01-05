using CVX_QLSX.App.Models;

namespace CVX_QLSX.App.Services;

/// <summary>
/// Interface for TCP communication service with the camera.
/// </summary>
public interface ITcpService
{
    /// <summary>
    /// Event raised when new production data is received from the camera.
    /// </summary>
    event EventHandler<ProductionData>? DataReceived;

    /// <summary>
    /// Event raised when connection status changes.
    /// </summary>
    event EventHandler<bool>? ConnectionStatusChanged;

    /// <summary>
    /// Current connection status.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connect to the camera.
    /// </summary>
    Task ConnectAsync(string ipAddress, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect from the camera.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Start the background reading task.
    /// </summary>
    Task StartReadingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the background reading task.
    /// </summary>
    Task StopReadingAsync();
}
