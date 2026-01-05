using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CVX_QLSX.App.Converters;

/// <summary>
/// Converts a boolean value to its inverse Visibility.
/// True = Collapsed, False = Visible
/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible;
        }
        return false;
    }
}

/// <summary>
/// Converts a boolean to a connection status color.
/// True = Green, False = Red
/// </summary>
public class BoolToConnectionColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ? "#4CAF50" : "#F44336";
        }
        return "#F44336";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts PlanStatus to bool for editing capability.
/// Waiting/Running = true (editable), Completed/Cancelled = false (read-only)
/// </summary>
public class StatusToEditableConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length > 0 && values[0] is CVX_QLSX.App.Models.PlanStatus status)
        {
            return status == CVX_QLSX.App.Models.PlanStatus.Waiting || 
                   status == CVX_QLSX.App.Models.PlanStatus.Running;
        }
        return true;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts DataGridRow to its row number (1-indexed).
/// Used for displaying STT (sequence number) column.
/// </summary>
public class RowNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Windows.Controls.DataGridRow row)
        {
            return row.GetIndex() + 1;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
