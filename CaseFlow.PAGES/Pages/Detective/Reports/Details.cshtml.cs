using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Reports;

[Authorize(Policy = "DetectiveOnly")]
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
}

