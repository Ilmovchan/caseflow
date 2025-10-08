using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Detectives;

public class DeleteModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public int Id { get; set; }

    public CaseFlow.DAL.Models.Detective Detective { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetDetectiveAsync(id);
        if (entity == null) return NotFound();
        Detective = entity;
        Id = id;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _adminService.DeleteDetectiveAsync(Id);
        return RedirectToPage("Index");
    }
}


