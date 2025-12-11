using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CVX_QLSX.App.Data;
using CVX_QLSX.App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using ClosedXML.Excel;

namespace CVX_QLSX.App.ViewModels;

public partial class ReportViewModel : ViewModelBase
{
    private readonly ProductionContext _context;

    [ObservableProperty]
    private DateTime _fromDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _toDate = DateTime.Today;

    public ObservableCollection<ProductionPlan> Plans { get; } = new();

    [ObservableProperty]
    private int _totalPlans;

    [ObservableProperty]
    private int _completedPlans;

    [ObservableProperty]
    private int _cancelledPlans;

    [ObservableProperty]
    private int _totalActualQuantity;

    [ObservableProperty]
    private string _exportStatus = string.Empty;

    public ReportViewModel(ProductionContext context)
    {
        _context = context;
        Title = "Báo cáo";
        _ = LoadReportAsync();
    }

    partial void OnFromDateChanged(DateTime value) => _ = LoadReportAsync();
    partial void OnToDateChanged(DateTime value) => _ = LoadReportAsync();

    [RelayCommand]
    private async Task LoadReportAsync()
    {
        try
        {
            IsBusy = true;
            Plans.Clear();

            var fromDateTime = FromDate.Date;
            var toDateTime = ToDate.Date.AddDays(1);

            var plans = await _context.ProductionPlans
                .Where(p => p.CreatedAt >= fromDateTime && p.CreatedAt < toDateTime)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            foreach (var plan in plans)
                Plans.Add(plan);

            TotalPlans = plans.Count;
            CompletedPlans = plans.Count(p => p.Status == PlanStatus.Completed);
            CancelledPlans = plans.Count(p => p.Status == PlanStatus.Cancelled);
            TotalActualQuantity = plans.Sum(p => p.CurrentQuantity);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (!Plans.Any())
        {
            ExportStatus = "Không có dữ liệu để xuất!";
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            DefaultExt = "xlsx",
            FileName = $"BaoCaoSanXuat_{FromDate:yyyyMMdd}_{ToDate:yyyyMMdd}.xlsx"
        };

        if (saveDialog.ShowDialog() != true)
            return;

        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Báo cáo Sản xuất");

            // Header styling
            worksheet.Cell(1, 1).Value = "BÁO CÁO SẢN XUẤT";
            worksheet.Range(1, 1, 1, 7).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(2, 1).Value = $"Từ ngày: {FromDate:dd/MM/yyyy} - Đến ngày: {ToDate:dd/MM/yyyy}";
            worksheet.Range(2, 1, 2, 7).Merge();
            worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Summary row
            worksheet.Cell(4, 1).Value = "Tổng KH:";
            worksheet.Cell(4, 2).Value = TotalPlans;
            worksheet.Cell(4, 3).Value = "Hoàn thành:";
            worksheet.Cell(4, 4).Value = CompletedPlans;
            worksheet.Cell(4, 5).Value = "Đã hủy:";
            worksheet.Cell(4, 6).Value = CancelledPlans;

            // Column headers
            int headerRow = 6;
            string[] headers = { "STT", "Sản phẩm", "SL Đặt", "Thực tế", "Ngày tạo", "Ngày hoàn thành", "Trạng thái" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(headerRow, i + 1).Value = headers[i];
                worksheet.Cell(headerRow, i + 1).Style.Font.Bold = true;
                worksheet.Cell(headerRow, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                worksheet.Cell(headerRow, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Data rows
            int row = headerRow + 1;
            int index = 1;
            foreach (var plan in Plans)
            {
                worksheet.Cell(row, 1).Value = index++;
                worksheet.Cell(row, 2).Value = plan.ProductName;
                worksheet.Cell(row, 3).Value = plan.TargetQuantity;
                worksheet.Cell(row, 4).Value = plan.CurrentQuantity;
                worksheet.Cell(row, 5).Value = plan.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(row, 6).Value = plan.CompletedAt?.ToString("dd/MM/yyyy HH:mm") ?? "-";
                worksheet.Cell(row, 7).Value = GetStatusText(plan.Status);
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders
            var dataRange = worksheet.Range(headerRow, 1, row - 1, headers.Length);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            workbook.SaveAs(saveDialog.FileName);
            ExportStatus = $"✓ Đã xuất file: {saveDialog.FileName}";
        }
        catch (Exception ex)
        {
            ExportStatus = $"✗ Lỗi: {ex.Message}";
        }
    }

    private static string GetStatusText(PlanStatus status) => status switch
    {
        PlanStatus.Waiting => "Chờ",
        PlanStatus.Running => "Đang chạy",
        PlanStatus.Completed => "Hoàn thành",
        PlanStatus.Cancelled => "Đã hủy",
        _ => "-"
    };
}
