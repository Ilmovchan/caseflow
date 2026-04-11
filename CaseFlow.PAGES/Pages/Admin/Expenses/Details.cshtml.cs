using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CaseFlow.PAGES.Pages.Admin.Expenses;

public class DetailsModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public Expense Expense { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetExpenseAsync(id);
        if (entity == null) return NotFound();
        Expense = entity;
        return Page();
    }

    public async Task<IActionResult> OnGetExportPdfAsync(int id)
    {
        var expense = await _adminService.GetExpenseAsync(id);
        if (expense == null) return NotFound();

        var connectedCase = await _adminService.GetCaseAsync(expense.CaseId);

        QuestPDF.Settings.License = LicenseType.Community;

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header()
                    .Text($"Видаток №{expense.Id}")
                    .SemiBold()
                    .FontSize(18);

                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text($"Сформовано: {DateTime.Now:yyyy-MM-dd HH:mm}");
                    column.Item().Text($"Пов’язана справа (ID): {expense.CaseId}");
                    column.Item().Text($"Назва пов’язаної справи: {connectedCase?.Title ?? "-"}");
                    column.Item().Text($"Мета: {expense.Purpose}");
                    column.Item().Text($"Сума: {expense.Amount:C}");
                    column.Item().Text($"Дата: {expense.DateTime:yyyy-MM-dd HH:mm}");
                    column.Item().Text($"Статус: {ToUkrainianStatus(expense.ApprovalStatus)}");
                    column.Item().PaddingTop(8).Text("Примітка:").SemiBold();
                    column.Item().Text(string.IsNullOrWhiteSpace(expense.Annotation) ? "-" : expense.Annotation);
                });
            });
        }).GeneratePdf();

        return File(pdfBytes, "application/pdf", $"expense-{expense.Id}.pdf");
    }

    private static string ToUkrainianStatus(ApprovalStatus status) => status switch
    {
        ApprovalStatus.Draft => "Чернетка",
        ApprovalStatus.Pending => "На розгляді",
        ApprovalStatus.Approved => "Підтверджено",
        ApprovalStatus.Declined => "Відхилено",
        _ => status.ToString()
    };
}
