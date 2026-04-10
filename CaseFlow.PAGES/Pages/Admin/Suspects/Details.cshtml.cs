using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Suspects;

public class DetailsModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public Suspect Suspect { get; set; } = null!;
    public List<Case> ConnectedCases { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetSuspectAsync(id);
        if (entity == null) return NotFound();
        Suspect = entity;

        ConnectedCases = (await _adminService.GetCasesBySuspectIdAsync(id))
            .OrderByDescending(c => c.StartDate)
            .ToList();

        return Page();
    }
}
