using AutoMapper;
using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Expenses;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<ExpenseDto> AllExpenses { get; set; } = new();
    public List<ExpenseDto> PendingExpenses { get; set; } = new();

    public async Task OnGetAsync()
    {
        var allExpenseEntities = await _adminService.GetExpensesAsync();
        var pendingExpenseEntities = await _adminService.GetPendingExpensesAsync();
        
        AllExpenses = _mapper.Map<List<ExpenseDto>>(allExpenseEntities);
        PendingExpenses = _mapper.Map<List<ExpenseDto>>(pendingExpenseEntities);
    }
}
