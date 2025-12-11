namespace CVX_QLSX.App.Services;

/// <summary>
/// Master counter service that handles camera reset scenarios using delta logic.
/// The camera is a "Session Counter" that resets when powered off/on.
/// This service maintains the "Master Count" that only resets on new shift.
/// </summary>
public class CounterService
{
    // App's master counters (persist across camera resets)
    private int _appTotal;
    private int _appOK;
    private int _appNG;

    // Last known camera values (to detect resets)
    private int _lastCameraTotal;
    private int _lastCameraOK;
    private int _lastCameraNG;

    // Track if we've received the first packet
    private bool _isFirstPacket = true;

    /// <summary>
    /// Current master total count (App's count).
    /// </summary>
    public int AppTotal => _appTotal;

    /// <summary>
    /// Current master OK count (App's count).
    /// </summary>
    public int AppOK => _appOK;

    /// <summary>
    /// Current master NG count (App's count).
    /// </summary>
    public int AppNG => _appNG;

    /// <summary>
    /// Event raised when counts are updated.
    /// </summary>
    public event EventHandler<CounterUpdatedEventArgs>? CountsUpdated;

    /// <summary>
    /// Processes incoming camera packet using delta logic.
    /// Handles camera power cycle resets gracefully.
    /// </summary>
    /// <param name="packet">The parsed camera packet</param>
    public void ProcessPacket(CameraPacket packet)
    {
        if (_isFirstPacket)
        {
            // First packet - only record baseline, don't add to counters
            // This ensures when new plan starts with camera at 45, 
            // we only count the DELTA (e.g., 45->46 = 1 item)
            _lastCameraTotal = packet.TotalCount;
            _lastCameraOK = packet.OKCount;
            _lastCameraNG = packet.NGCount;
            _isFirstPacket = false;
            
            System.Diagnostics.Debug.WriteLine(
                $"[CounterService] First packet - baseline set: Total={packet.TotalCount}, OK={packet.OKCount}, NG={packet.NGCount}");
            
            // Don't add anything to counters - just record baseline
            // Counts will only increment when camera values change
        }
        else
        {
            // Calculate deltas
            int deltaTotal = CalculateDelta(packet.TotalCount, _lastCameraTotal);
            int deltaOK = CalculateDelta(packet.OKCount, _lastCameraOK);
            int deltaNG = CalculateDelta(packet.NGCount, _lastCameraNG);

            // Update app counters
            _appTotal += deltaTotal;
            _appOK += deltaOK;
            _appNG += deltaNG;

            // Update last camera values
            _lastCameraTotal = packet.TotalCount;
            _lastCameraOK = packet.OKCount;
            _lastCameraNG = packet.NGCount;
        }

        // Notify listeners
        OnCountsUpdated();
    }

    /// <summary>
    /// Calculates delta between current and last camera value.
    /// If delta is negative (camera reset), assumes delta equals current value.
    /// </summary>
    private static int CalculateDelta(int currentCameraValue, int lastCameraValue)
    {
        int delta = currentCameraValue - lastCameraValue;
        
        // If delta < 0, camera was reset - treat current value as the delta
        if (delta < 0)
        {
            delta = currentCameraValue;
            System.Diagnostics.Debug.WriteLine(
                $"[CounterService] Camera reset detected! Last={lastCameraValue}, Current={currentCameraValue}, Using delta={delta}");
        }

        return delta;
    }

    /// <summary>
    /// Resets all counters to zero. Called when starting a new shift.
    /// </summary>
    public void ResetForNewShift()
    {
        _appTotal = 0;
        _appOK = 0;
        _appNG = 0;
        _lastCameraTotal = 0;
        _lastCameraOK = 0;
        _lastCameraNG = 0;
        _isFirstPacket = true;

        System.Diagnostics.Debug.WriteLine("[CounterService] Counters reset for new shift");
        OnCountsUpdated();
    }

    /// <summary>
    /// Restores counters from saved state (e.g., from database on app restart).
    /// </summary>
    public void RestoreState(int savedTotal, int savedOK, int savedNG)
    {
        _appTotal = savedTotal;
        _appOK = savedOK;
        _appNG = savedNG;
        _isFirstPacket = true; // Treat next camera packet as first

        System.Diagnostics.Debug.WriteLine(
            $"[CounterService] State restored: Total={savedTotal}, OK={savedOK}, NG={savedNG}");
        OnCountsUpdated();
    }

    private void OnCountsUpdated()
    {
        CountsUpdated?.Invoke(this, new CounterUpdatedEventArgs
        {
            Total = _appTotal,
            OK = _appOK,
            NG = _appNG
        });
    }
}

/// <summary>
/// Event args for counter updates.
/// </summary>
public class CounterUpdatedEventArgs : EventArgs
{
    public int Total { get; set; }
    public int OK { get; set; }
    public int NG { get; set; }
}
