using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Evidence;

public class DetailsModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public DAL.Models.Evidence Evidence { get; set; } = null!;
    public List<Case> ConnectedCases { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetEvidenceAsync(id);
        if (entity == null) return NotFound();
        Evidence = entity;

        ConnectedCases = (await _adminService.GetCasesByEvidenceIdAsync(id))
            .OrderByDescending(c => c.StartDate)
            .ToList();

        return Page();
    }
}
