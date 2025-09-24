using CaseFlow.BLL.Dto.CaseType;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.CaseTypes;

public class EditModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public UpdateCaseTypeDto Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetCaseTypeAsync(id);
        if (entity == null) return NotFound();

        Id = entity.Id;
        Input = new UpdateCaseTypeDto
        {
            Name = entity.Name,
            Price = entity.Price
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var updated = await _adminService.UpdateCaseTypeAsync(Id, Input);
        return RedirectToPage("Details", new { id = updated.Id });
    }
}
