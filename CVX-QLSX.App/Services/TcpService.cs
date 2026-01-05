using System.Net.Sockets;
using System.Text;
using CVX_QLSX.App.Models;

namespace CVX_QLSX.App.Services;

/// <summary>
/// TCP Client service for camera communication with auto-reconnect.
/// </summary>
public class TcpService : ITcpService, IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _readingCts;
    private Task? _readingTask;
    private bool _isDisposed;

    private string _ipAddress = string.Empty;
    private int _port;
    private readonly int _reconnectDelayMs = 5000;
    private bool _shouldReconnect = true;

    public event EventHandler<ProductionData>? DataReceived;
    public event EventHandler<bool>? ConnectionStatusChanged;

    public bool IsConnected => _client?.Connected ?? false;

    /// <summary>
    /// Connect to the camera TCP server.
    /// </summary>
    public async Task ConnectAsync(string ipAddress, int port, CancellationToken cancellationToken = default)
    {
        _ipAddress = ipAddress;
        _port = port;
        _shouldReconnect = true; // Reset reconnect flag when manually connecting

        await InternalConnectAsync(cancellationToken);
    }

    private async Task InternalConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            Disconnect();

            _client = new TcpClient();
            
            // Connect with timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await _client.ConnectAsync(_ipAddress, _port, linkedCts.Token);
            _stream = _client.GetStream();

            OnConnectionStatusChanged(true);
            
            System.Diagnostics.Debug.WriteLine($"[TcpService] Connected to {_ipAddress}:{_port}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TcpService] Connection failed: {ex.Message}");
            OnConnectionStatusChanged(false);
            throw;
        }
    }

    /// <summary>
    /// Start the background reading task with auto-reconnect.
    /// </summary>
    public Task StartReadingAsync(CancellationToken cancellationToken = default)
    {
        _readingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _readingTask = Task.Run(() => ReadLoopAsync(_readingCts.Token), _readingCts.Token);
        return Task.CompletedTask;
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Attempt reconnect if not connected
                if (!IsConnected && _shouldReconnect)
                {
                    await ReconnectAsync(cancellationToken);
                }

                if (_stream == null || !IsConnected)
                {
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                // Read data from stream
                int bytesRead = await _stream.ReadAsync(buffer, cancellationToken);
                
                if (bytesRead == 0)
                {
                    // Connection closed by server
                    System.Diagnostics.Debug.WriteLine("[TcpService] Connection closed by server.");
                    OnConnectionStatusChanged(false);
                    continue;
                }

                // Process immediately - no newline delimiter expected
                string data = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                
                if (!string.IsNullOrWhiteSpace(data))
                {
                    System.Diagnostics.Debug.WriteLine($"[TcpService] Raw data: {data}");
                    ProcessMessage(data);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TcpService] Read error: {ex.Message}");
                OnConnectionStatusChanged(false);
                
                if (_shouldReconnect)
                {
                    await Task.Delay(_reconnectDelayMs, cancellationToken);
                }
            }
        }
    }

    private async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        int retryCount = 0;
        int maxRetries = 10;
        int delay = _reconnectDelayMs;

        while (!cancellationToken.IsCancellationRequested && retryCount < maxRetries)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[TcpService] Reconnecting... Attempt {retryCount + 1}");
                await InternalConnectAsync(cancellationToken);
                return; // Success
            }
            catch
            {
                retryCount++;
                delay = Math.Min(delay * 2, 60000); // Exponential backoff, max 60s
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private void ProcessMessage(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[TcpService] Received: {message}");

        if (ProductionData.TryParse(message, out var data) && data != null)
        {
            DataReceived?.Invoke(this, data);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[TcpService] Failed to parse: {message}");
        }
    }

    /// <summary>
    /// Stop the background reading task.
    /// </summary>
    public async Task StopReadingAsync()
    {
        _shouldReconnect = false;
        _readingCts?.Cancel();

        if (_readingTask != null)
        {
            try
            {
                await _readingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
    }

    /// <summary>
    /// Disconnect from the camera.
    /// </summary>
    public async Task DisconnectAsync()
    {
        await StopReadingAsync();
        Disconnect();
    }

    private void Disconnect()
    {
        _stream?.Close();
        _stream?.Dispose();
        _stream = null;

        _client?.Close();
        _client?.Dispose();
        _client = null;

        OnConnectionStatusChanged(false);
    }

    private void OnConnectionStatusChanged(bool isConnected)
    {
        ConnectionStatusChanged?.Invoke(this, isConnected);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _readingCts?.Cancel();
        _readingCts?.Dispose();
        Disconnect();

        GC.SuppressFinalize(this);
    }
}
