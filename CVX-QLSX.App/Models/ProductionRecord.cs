using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CVX_QLSX.App.Models;

/// <summary>
/// Represents a production record from the camera system.
/// </summary>
public class ProductionRecord
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Timestamp when the record was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Foreign key to the shift this record belongs to
    /// </summary>
    public int? ShiftId { get; set; }

    /// <summary>
    /// Product identifier from the camera
    /// </summary>
    [MaxLength(100)]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Total count of items
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Count of OK (passed) items
    /// </summary>
    public int OK { get; set; }

    /// <summary>
    /// Count of NG (failed) items
    /// </summary>
    public int NG { get; set; }

    /// <summary>
    /// Manufacturing date from camera data
    /// </summary>
    [MaxLength(20)]
    public string MfgDate { get; set; } = string.Empty;

    /// <summary>
    /// Expiration date from camera data
    /// </summary>
    [MaxLength(20)]
    public string ExpDate { get; set; } = string.Empty;

    // Navigation property
    [ForeignKey(nameof(ShiftId))]
    public virtual ShiftSetting? Shift { get; set; }
}
