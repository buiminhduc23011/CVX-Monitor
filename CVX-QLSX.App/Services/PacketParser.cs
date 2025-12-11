using System.Globalization;

namespace CVX_QLSX.App.Services;

/// <summary>
/// Parsed packet from camera TCP stream.
/// Format: TotalCount,OKCount,NGCount,ProductID,MfgDate(ddMMyy),ExpDate(ddMMyy)
/// Example: "10,12,2,Hehe,111225,110626"
/// </summary>
public class CameraPacket
{
    public int TotalCount { get; set; }
    public int OKCount { get; set; }
    public int NGCount { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public DateTime? MfgDate { get; set; }
    public DateTime? ExpDate { get; set; }
    public string RawMfgDate { get; set; } = string.Empty;
    public string RawExpDate { get; set; } = string.Empty;
}

/// <summary>
/// Parses camera TCP data packets.
/// </summary>
public static class PacketParser
{
    private const string DateFormat = "ddMMyy";

    /// <summary>
    /// Parses a CSV string from the camera into a CameraPacket.
    /// Format: "TotalCount,OKCount,NGCount,ProductID,MfgDate,ExpDate"
    /// Example: "10,12,2,Hehe,111225,110626"
    /// </summary>
    public static bool TryParse(string rawData, out CameraPacket? packet)
    {
        packet = null;

        if (string.IsNullOrWhiteSpace(rawData))
            return false;

        var parts = rawData.Trim().Split(',');
        if (parts.Length != 6)
            return false;

        // Parse counts
        if (!int.TryParse(parts[0].Trim(), out int total))
            return false;
        if (!int.TryParse(parts[1].Trim(), out int ok))
            return false;
        if (!int.TryParse(parts[2].Trim(), out int ng))
            return false;

        // Parse dates (ddMMyy format)
        string rawMfg = parts[4].Trim();
        string rawExp = parts[5].Trim();
        
        DateTime? mfgDate = ParseDate(rawMfg);
        DateTime? expDate = ParseDate(rawExp);

        packet = new CameraPacket
        {
            TotalCount = total,
            OKCount = ok,
            NGCount = ng,
            ProductId = parts[3].Trim(),
            MfgDate = mfgDate,
            ExpDate = expDate,
            RawMfgDate = rawMfg,
            RawExpDate = rawExp
        };

        return true;
    }

    /// <summary>
    /// Parses a date string in ddMMyy format.
    /// Example: "111225" -> December 11, 2025
    /// </summary>
    private static DateTime? ParseDate(string dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr) || dateStr.Length != 6)
            return null;

        if (DateTime.TryParseExact(dateStr, DateFormat, 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Formats a DateTime to ddMMyy string.
    /// </summary>
    public static string FormatDate(DateTime? date)
    {
        return date?.ToString(DateFormat) ?? "-";
    }

    /// <summary>
    /// Formats a DateTime to display format (dd/MM/yyyy).
    /// </summary>
    public static string FormatDateDisplay(DateTime? date)
    {
        return date?.ToString("dd/MM/yyyy") ?? "-";
    }
}
