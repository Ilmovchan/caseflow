using AutoMapper;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CaseFlow.PAGES.Pages.Detective.Expenses;

[Authorize(Policy = "DetectiveOnly")]
[IgnoreAntiforgeryToken]
public class IndexModel(DetectiveService detectiveService, IMapper mapper) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;
    private readonly IMapper _mapper = mapper;

    public List<ExpenseDto> Expenses { get; set; } = new();
    public PagedResult<ExpenseDto> PagedExpenses { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;

        var allExpenses = await _detectiveService.GetExpensesAsync();
        
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            allExpenses = allExpenses.Where(e =>
                e.Id.ToString().Contains(term) ||
                e.CaseId.ToString().Contains(term) ||
                (!string.IsNullOrEmpty(e.Purpose) && e.Purpose.ToLower().Contains(term)) ||
                e.Amount.ToString().Contains(term) ||
                e.DateTime.ToString().Contains(term) ||
                (e.ApprovalStatus?.ToString().ToLower() ?? "").Contains(term))
                .ToList();
        }

        var totalCount = allExpenses.Count;
        var items = allExpenses
            .OrderBy(e => e.Id)
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        PagedExpenses = new PagedResult<ExpenseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = PageSize
        };

        Expenses = PagedExpenses.Items;
    }

    public async Task<IActionResult> OnPostApproveAsync([FromQuery] int id)
    {
        try
        {
            var submitted = await _detectiveService.SubmitExpenseAsync(id);
            return new JsonResult(new {
                success = true,
                newStatus = "Pending",
                newStatusText = "Очікує",
                newStatusColor = "warning",
                approvalStatus = "Pending",
                message = "Видатки надіслано на перевірку!",
                expense = submitted
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new {
                success = false,
                error = ex.Message
            });
        }
    }

    public async Task<IActionResult> OnPostRejectAsync([FromQuery] int id)
    {
        try
        {
            await _detectiveService.DeleteExpenseAsync(id);
            return new JsonResult(new {
                success = true,
                message = "Видатки видалено успішно"
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new {
                success = false,
                error = ex.Message
            });
        }
    }
}

