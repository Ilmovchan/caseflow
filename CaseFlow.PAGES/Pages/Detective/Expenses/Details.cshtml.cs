using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Expenses;

[Authorize(Policy = "DetectiveOnly")]
public class DetailsModel(DetectiveService detectiveService) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;

    public ExpenseDto Expense { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _detectiveService.GetExpenseAsync(id);
        if (entity == null) return NotFound();
        Expense = entity;
        return Page();
    }
}

