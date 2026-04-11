using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CaseFlow.PAGES.Pages.Admin.Reports;

public class DetailsModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public Report Report { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetReportAsync(id);
        if (entity == null) return NotFound();
        Report = entity;
        return Page();
    }

    public async Task<IActionResult> OnGetExportPdfAsync(int id)
    {
        var report = await _adminService.GetReportAsync(id);
        if (report == null) return NotFound();

        var connectedCase = await _adminService.GetCaseAsync(report.CaseId);

        QuestPDF.Settings.License = LicenseType.Community;

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header()
                    .Text($"Звіт №{report.Id}")
                    .SemiBold()
                    .FontSize(18);

                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text($"Сформовано: {DateTime.Now:yyyy-MM-dd HH:mm}");
                    column.Item().Text($"Пов’язана справа (ID): {report.CaseId}");
                    column.Item().Text($"Назва пов’язаної справи: {connectedCase?.Title ?? "-"}");
                    column.Item().Text($"Дата звіту: {report.ReportDate:yyyy-MM-dd HH:mm}");
                    column.Item().Text($"Статус: {ToUkrainianStatus(report.ApprovalStatus)}");
                    column.Item().Text("Резюме:").SemiBold();
                    column.Item().Text(report.Summary ?? "-");
                    column.Item().PaddingTop(8).Text("Коментарі:").SemiBold();
                    column.Item().Text(string.IsNullOrWhiteSpace(report.Comments) ? "-" : report.Comments);
                });
            });
        }).GeneratePdf();

        return File(pdfBytes, "application/pdf", $"Report_{report.Id}_{report.ReportDate}.pdf");
    }

    private static string ToUkrainianStatus(CaseFlow.DAL.Enums.ApprovalStatus status) => status switch
    {
        CaseFlow.DAL.Enums.ApprovalStatus.Draft => "Чернетка",
        CaseFlow.DAL.Enums.ApprovalStatus.Pending => "На розгляді",
        CaseFlow.DAL.Enums.ApprovalStatus.Approved => "Підтверджено",
        CaseFlow.DAL.Enums.ApprovalStatus.Declined => "Відхилено",
        _ => status.ToString()
    };
}
