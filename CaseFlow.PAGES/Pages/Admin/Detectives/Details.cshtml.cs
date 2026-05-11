using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Detectives;

[Authorize(Policy = "AdminOnly")]
public class DetailsModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public CaseFlow.DAL.Models.Detective Detective { get; set; } = null!;
    public List<Case> ConnectedCases { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetDetectiveAsync(id);
        if (entity == null) return NotFound();
        Detective = entity;

        ConnectedCases = (await _adminService.GetCasesAsync())
            .Where(c => c.DetectiveId == id)
            .OrderByDescending(c => c.StartDate)
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostUnlinkCaseAsync(int id, int caseId)
    {
        var detective = await _adminService.GetDetectiveAsync(id);
        if (detective == null)
            return NotFound();

        var caseEntity = await _adminService.GetCaseAsync(caseId);
        if (caseEntity == null || caseEntity.DetectiveId != id)
            return NotFound();

        await _adminService.DismissDetectiveAsync(caseId);
        return RedirectToPage("./Details", new { id });
    }
}


