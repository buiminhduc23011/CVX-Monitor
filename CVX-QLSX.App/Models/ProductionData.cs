namespace CVX_QLSX.App.Models;

/// <summary>
/// Parsed production data from TCP camera stream.
/// Format: TotalCount,OKCount,NGCount,ProductID
/// MfgDate is automatically set to today's date.
/// </summary>
public class ProductionData
{
    public int TotalCount { get; set; }
    public int OKCount { get; set; }
    public int NGCount { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string MfgDate { get; set; } = string.Empty;
    public string ExpDate { get; set; } = string.Empty;

    /// <summary>
    /// Parses a CSV string from the camera into ProductionData.
    /// Expected format: "TotalCount,OKCount,NGCount,ProductID"
    /// MfgDate is automatically set to today's date in yyyy-MM-dd format.
    /// </summary>
    public static bool TryParse(string csvData, out ProductionData? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(csvData))
            return false;

        var parts = csvData.Trim().Split(',');
        if (parts.Length != 4)
            return false;

        if (!int.TryParse(parts[0], out int total) ||
            !int.TryParse(parts[1], out int ok) ||
            !int.TryParse(parts[2], out int ng))
        {
            return false;
        }

        result = new ProductionData
        {
            TotalCount = total,
            OKCount = ok,
            NGCount = ng,
            ProductId = parts[3].Trim(),
            MfgDate = DateTime.Now.ToString("yyyy-MM-dd"),
            ExpDate = string.Empty
        };

        return true;
    }
}
