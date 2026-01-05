namespace CVX_QLSX.App.Models;

/// <summary>
/// Application settings stored locally (JSON file).
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Camera TCP/IP address
    /// </summary>
    public string CameraIpAddress { get; set; } = "192.168.0.10";

    /// <summary>
    /// Camera TCP port
    /// </summary>
    public int CameraPort { get; set; } = 8500;

    /// <summary>
    /// Auto-reconnect interval in seconds
    /// </summary>
    public int ReconnectIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Enable auto-reconnect on connection loss
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// List of available products for production plans
    /// </summary>
    public List<string> ProductList { get; set; } = new()
    {
        "Chay Dong Co",
        "Lau Tom Chua Cay Cung Dinh",
    };
}
