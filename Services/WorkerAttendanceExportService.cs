using System.Globalization;
using System.Text;
using ChurchApp.Models;
using ClosedXML.Excel;

namespace ChurchApp.Services;

public sealed class WorkerAttendanceExportService
{
    public byte[] CreateCsv(WorkerAttendanceExportRequest request)
    {
        var columns = GetServiceColumns(request.SelectedService);
        var sb = new StringBuilder();

        var headers = new List<string>
        {
            "Worker", "Role", "Category", "Directorate", "Department"
        };
        headers.AddRange(columns.Select(x => x.Header + " %"));
        headers.Add("MTD %");
        headers.Add("Grade");
        sb.AppendLine(string.Join(',', headers.Select(Csv)));

        foreach (var worker in request.Results)
        {
            var row = new List<string>
            {
                worker.WorkerName,
                worker.WorkerRole,
                worker.Category == AttendancePersonCategory.Leader ? "Leader" : "Worker",
                worker.DirectorateName,
                worker.DepartmentName
            };

            row.AddRange(columns.Select(c => GetServicePercentageText(worker, c.Keyword)));
            row.Add(worker.AvailableWeight > 0 ? worker.MonthToDatePercentage.ToString("0.##", CultureInfo.InvariantCulture) : "");
            row.Add(worker.Grade == AttendanceGrade.NoOpportunity ? "Not Due" : worker.Grade.ToString());

            sb.AppendLine(string.Join(',', row.Select(Csv)));
        }

        return new UTF8Encoding(true).GetBytes(sb.ToString());
    }

    public byte[] CreateExcel(WorkerAttendanceExportRequest request)
    {
        var columns = GetServiceColumns(request.SelectedService);
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Attendance Report");

        var totalColumns = 7 + columns.Count;
        ws.Cell(1, 1).Value = "BCC SERVICEHUB - WORKER ATTENDANCE REPORT";
        ws.Range(1, 1, 1, totalColumns).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(2, 1).Value = $"Period: {request.MonthName} {request.Year}";
        ws.Range(2, 1, 2, totalColumns).Merge();
        ws.Cell(3, 1).Value = BuildFilterDescription(request);
        ws.Range(3, 1, 3, totalColumns).Merge();
        ws.Cell(4, 1).Value = $"Generated: {request.GeneratedAt:dd MMM yyyy, hh:mm tt}";
        ws.Range(4, 1, 4, totalColumns).Merge();

        var headers = new List<string> { "Worker", "Role", "Category", "Directorate", "Department" };
        headers.AddRange(columns.Select(x => x.Header + " %"));
        headers.Add("MTD %");
        headers.Add("Grade");

        const int headerRow = 6;
        for (var i = 0; i < headers.Count; i++)
            ws.Cell(headerRow, i + 1).Value = headers[i];

        var headerRange = ws.Range(headerRow, 1, headerRow, headers.Count);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        var row = headerRow + 1;
        foreach (var worker in request.Results)
        {
            var col = 1;
            ws.Cell(row, col++).Value = worker.WorkerName;
            ws.Cell(row, col++).Value = worker.WorkerRole;
            ws.Cell(row, col++).Value = worker.Category == AttendancePersonCategory.Leader ? "Leader" : "Worker";
            ws.Cell(row, col++).Value = worker.DirectorateName;
            ws.Cell(row, col++).Value = worker.DepartmentName;

            foreach (var serviceColumn in columns)
            {
                var value = GetServicePercentage(worker, serviceColumn.Keyword);
                if (value.HasValue)
                {
                    ws.Cell(row, col).Value = value.Value;
                    ws.Cell(row, col).Style.NumberFormat.Format = "0.00";
                }
                else
                {
                    ws.Cell(row, col).Value = "—";
                }
                col++;
            }

            if (worker.AvailableWeight > 0)
            {
                ws.Cell(row, col).Value = worker.MonthToDatePercentage;
                ws.Cell(row, col).Style.NumberFormat.Format = "0.00";
            }
            else
            {
                ws.Cell(row, col).Value = "—";
            }
            col++;

            ws.Cell(row, col).Value = worker.Grade == AttendanceGrade.NoOpportunity ? "Not Due" : worker.Grade.ToString();
            row++;
        }

        var used = ws.Range(headerRow, 1, Math.Max(headerRow, row - 1), headers.Count);
        used.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        used.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        used.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.SheetView.FreezeRows(headerRow);
        ws.Columns().AdjustToContents();
        foreach (var column in ws.ColumnsUsed())
        {
            if (column.Width > 32)
                column.Width = 32;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string BuildFilterDescription(WorkerAttendanceExportRequest r)
    {
        var search = string.IsNullOrWhiteSpace(r.SearchTerm) ? "None" : r.SearchTerm.Trim();
        return $"Category: {DisplayAll(r.SelectedCategory, "All")} | Directorate: {DisplayAll(r.SelectedDirectorate, "All Directorates")} | Department: {DisplayAll(r.SelectedDepartment, "All Departments")} | Service: {DisplayAll(r.SelectedService, "All Services")} | Grade: {DisplayAll(r.SelectedGrade, "All Grades")} | Search: {search}";
    }

    private static string DisplayAll(string value, string allText) =>
        string.IsNullOrWhiteSpace(value) || value.Equals("All", StringComparison.OrdinalIgnoreCase) ? allText : value;

    private static List<ServiceColumn> GetServiceColumns(string selectedService)
    {
        var all = new List<ServiceColumn>
        {
            new("Sunday", "Sunday"),
            new("Mid Week", "Mid Week"),
            new("Communion", "Communion"),
            new("Prayer Rain", "Prayer")
        };

        if (string.IsNullOrWhiteSpace(selectedService) || selectedService.Equals("All", StringComparison.OrdinalIgnoreCase))
            return all;

        return all.Where(x => selectedService.Contains(x.Keyword, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private static decimal? GetServicePercentage(WorkerMonthlyAttendanceScore worker, string keyword)
    {
        var service = worker.ServiceScores.FirstOrDefault(x =>
            x.ServiceName.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        return service == null || service.AvailableWeight <= 0 ? null : service.ServicePercentage;
    }

    private static string GetServicePercentageText(WorkerMonthlyAttendanceScore worker, string keyword)
    {
        var value = GetServicePercentage(worker, keyword);
        return value?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string GetServicePercentageDisplay(WorkerMonthlyAttendanceScore worker, string keyword)
    {
        var value = GetServicePercentage(worker, keyword);
        return value.HasValue ? $"{value.Value:0.##}%" : "—";
    }

    private static string EmptyAsDash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }

    private sealed record ServiceColumn(string Header, string Keyword);
}

public sealed class WorkerAttendanceExportRequest
{
    public required IReadOnlyCollection<WorkerMonthlyAttendanceScore> Results { get; init; }
    public required int Year { get; init; }
    public required string MonthName { get; init; }
    public required string SelectedCategory { get; init; }
    public required string SelectedService { get; init; }
    public required string SelectedDirectorate { get; init; }
    public required string SelectedDepartment { get; init; }
    public required string SelectedGrade { get; init; }
    public string SearchTerm { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
}

