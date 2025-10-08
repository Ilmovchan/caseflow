using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Cases;

public class DetailsModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public Case Case { get; set; } = null!;
    public List<CaseFlow.DAL.Models.Detective> UnassignedDetectives { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetCaseAsync(id);
        if (entity == null) return NotFound();
        Case = entity;
        UnassignedDetectives = await _adminService.GetUnassignedDetectivesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(int id, int detectiveId)
    {
        await _adminService.AssignDetectiveAsync(id, detectiveId);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDismissAsync(int id)
    {
        await _adminService.DismissDetectiveAsync(id);
        return RedirectToPage(new { id });
    }
}


