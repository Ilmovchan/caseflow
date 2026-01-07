using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Cases;

public class DetailsModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public Case Case { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetCaseAsync(id);
        if (entity == null) return NotFound();
        Case = entity;
        return Page();
    }
}


