using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace CVX_QLSX.App.Models;

/// <summary>
/// Represents a production plan in the queue.
/// </summary>
public class ProductionPlan : INotifyPropertyChanged
{
    private int _currentQuantity;
    private int _targetQuantity;
    private PlanStatus _status = PlanStatus.Waiting;

    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string ProductName { get; set; } = string.Empty;

    public int TargetQuantity
    {
        get => _targetQuantity;
        set { _targetQuantity = value; OnPropertyChanged(); }
    }

    public int CurrentQuantity
    {
        get => _currentQuantity;
        set { _currentQuantity = value; OnPropertyChanged(); }
    }

    public int QueueOrder { get; set; }

    public PlanStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public enum PlanStatus
{
    Waiting,
    Running,
    Completed,
    Cancelled
}
