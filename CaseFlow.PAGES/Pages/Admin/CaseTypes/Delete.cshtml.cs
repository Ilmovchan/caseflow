using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.CaseTypes;

public class DeleteModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public int Id { get; set; }

    public CaseType CaseType { get; set; } = null!;

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetCaseTypeAsync(id);
        if (entity == null) return NotFound();
        CaseType = entity;
        Id = id;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            await _adminService.DeleteCaseTypeAsync(Id);
            return RedirectToPage("Index");
        }
        catch (EntityDeleteConflictException ex)
        {
            ErrorMessage = ex.Message;
            var entity = await _adminService.GetCaseTypeAsync(Id);
            if (entity == null) return RedirectToPage("Index");
            CaseType = entity;
            return Page();
        }
    }
}
