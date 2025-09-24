using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Expenses;

public class IndexModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public List<Expense> AllExpenses { get; set; } = new();
    public List<Expense> PendingExpenses { get; set; } = new();

    public async Task OnGetAsync()
    {
        AllExpenses = await _adminService.GetExpensesAsync();
        PendingExpenses = await _adminService.GetPendingExpensesAsync();
    }
}
