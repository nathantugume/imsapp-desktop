using System.Collections.Generic;
using System.Threading.Tasks;
using imsapp_desktop.Models;

namespace imsapp_desktop.Services;

public interface IReportService
{
    Task<(IReadOnlyList<ReportRow> Rows, ReportSummary Summary, string Title, string Period)> GetDailyReportAsync(string date);
    Task<(IReadOnlyList<ReportRow> Rows, ReportSummary Summary, string Title, string Period)> GetMonthlyReportAsync(string yearMonth);
    Task<(IReadOnlyList<ReportRow> Rows, ReportSummary Summary, string Title, string Period)> GetYearlyReportAsync(int year);
}
