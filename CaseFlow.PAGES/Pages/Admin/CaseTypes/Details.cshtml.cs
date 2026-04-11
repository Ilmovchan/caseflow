using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.CaseTypes;

public class DetailsModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public CaseType CaseType { get; set; } = null!;
    public List<Case> ConnectedCases { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetCaseTypeAsync(id);
        if (entity == null) return NotFound();
        CaseType = entity;
        ConnectedCases = await _adminService.GetCasesByCaseTypeIdAsync(id);
        return Page();
    }
}
