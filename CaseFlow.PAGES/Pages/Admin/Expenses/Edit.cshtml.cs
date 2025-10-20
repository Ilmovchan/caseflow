using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Expenses;

public class EditModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public UpdateExpenseDto Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetExpenseAsync(id);
        if (entity == null) return NotFound();

        Id = entity.Id;
        Input = new UpdateExpenseDto
        {
            DateTime = entity.DateTime,
            Purpose = entity.Purpose,
            Amount = entity.Amount,
            Annotation = entity.Annotation,
            Status = entity.ApprovalStatus
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var updated = await _adminService.UpdateExpenseAsync(Id, Input);
        return RedirectToPage("Details", new { id = updated.Id });
    }
}

