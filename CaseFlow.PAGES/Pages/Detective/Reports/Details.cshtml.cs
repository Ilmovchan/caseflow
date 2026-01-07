using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CaseFlow.PAGES.Pages.Detective.Reports;

[Authorize(Policy = "DetectiveOnly")]
[IgnoreAntiforgeryToken]
public class DetailsModel(DetectiveService detectiveService) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;

    public ReportDto Report { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _detectiveService.GetReportAsync(id);
        if (entity == null) return NotFound();
        Report = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostSubmitAsync([FromRoute] int id)
    {
        try
        {
            var submitted = await _detectiveService.SubmitReportAsync(id);
            return new JsonResult(new {
                success = true,
                newStatus = "Pending",
                newStatusText = "Очікує",
                newStatusColor = "warning",
                approvalStatus = "Pending",
                message = "Звіт надіслано на перевірку!",
                report = submitted
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new {
                success = false,
                error = ex.Message
            });
        }
    }
}

