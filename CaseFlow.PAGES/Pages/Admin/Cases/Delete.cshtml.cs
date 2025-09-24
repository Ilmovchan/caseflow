using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Cases;

public class DeleteModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public int Id { get; set; }

    public Case Case { get; set; } = null!;

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetCaseAsync(id);
        if (entity == null) return NotFound();
        Case = entity;
        Id = id;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            await _adminService.DeleteCaseAsync(Id);
            return RedirectToPage("Index");
        }
        catch (EntityDeleteConflictException ex)
        {
            ErrorMessage = ex.Message;
            var entity = await _adminService.GetCaseAsync(Id);
            if (entity == null) return RedirectToPage("Index");
            Case = entity;
            return Page();
        }
    }
}


