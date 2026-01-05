using System.ComponentModel.DataAnnotations;

namespace CVX_QLSX.App.Models;

/// <summary>
/// Represents a work shift configuration with target output.
/// </summary>
public class ShiftSetting
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Name of the shift (e.g., "Morning", "Afternoon", "Night")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ShiftName { get; set; } = string.Empty;

    /// <summary>
    /// Start time of the shift
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// End time of the shift
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Target output for this shift (Sản lượng mục tiêu)
    /// </summary>
    public int TargetOutput { get; set; }

    /// <summary>
    /// Whether this shift is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation property
    public virtual ICollection<ProductionRecord> ProductionRecords { get; set; } = new List<ProductionRecord>();
}
