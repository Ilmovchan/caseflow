using AutoMapper;
using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CaseFlow.PAGES.Pages.Admin.Expenses;

[IgnoreAntiforgeryToken]
public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<ExpenseDto> AllExpenses { get; set; } = new();
    public List<ExpenseDto> PendingExpenses { get; set; } = new();
    public PagedResult<ExpenseDto> PagedExpenses { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;

        // Get all expenses first
        List<Expense> allExpenses;
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchResult = await _adminService.SearchExpensesPagedAsync(SearchTerm, 1, int.MaxValue);
            allExpenses = searchResult.Items;
        }
        else
        {
            var allResult = await _adminService.GetExpensesPagedAsync(1, int.MaxValue);
            allExpenses = allResult.Items;
        }

        // Filter: Admin sees submitted workflow items (exclude Draft only)
        var filteredItems = allExpenses
            .Where(e => e.ApprovalStatus != ApprovalStatus.Draft)
            .ToList();

        // Apply pagination to filtered items
        var totalCount = filteredItems.Count;
        var items = filteredItems
            .OrderBy(e => e.Id)
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        PagedExpenses = new PagedResult<ExpenseDto>
        {
            Items = _mapper.Map<List<ExpenseDto>>(items),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = PageSize
        };

        AllExpenses = PagedExpenses.Items;

        // Get pending expenses for approval section
        var pendingExpenseEntities = await _adminService.GetPendingExpensesAsync();
        PendingExpenses = _mapper.Map<List<ExpenseDto>>(pendingExpenseEntities);
    }
    
    public async Task<IActionResult> OnPostApproveAsync([FromQuery] int id)
    {
        try
        {
            var approved = await _adminService.ApproveExpenseAsync(id);
            return new JsonResult(new { 
                success = true, 
                newStatus = "Approved", 
                newStatusText = "Схвалено", 
                newStatusColor = "success",
                approvalStatus = "Approved"
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
            var rejected = await _adminService.RejectExpenseAsync(id);
            return new JsonResult(new {
                success = true,
                newStatus = "Declined",
                newStatusText = "Відхилено",
                newStatusColor = "danger",
                approvalStatus = "Declined"
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

    public async Task<IActionResult> OnPostSubmitAsync([FromQuery] int id)
    {
        try
        {
            var submitted = await _adminService.SubmitExpenseAsync(id);
            return new JsonResult(new {
                success = true,
                newStatus = "Pending",
                newStatusText = "Очікує",
                newStatusColor = "warning",
                approvalStatus = "Pending",
                message = "Видатки надіслано на перевірку!",
                // keep response lightweight (frontend only needs status fields)
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

    public async Task<IActionResult> OnPostDeleteAsync([FromQuery] int id)
    {
        try
        {
            await _adminService.DeleteExpenseAsync(id);
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
    
    private List<ExpenseDto> GenerateSampleExpenses()
    {
        var sampleExpenses = new List<ExpenseDto>();
        var random = new Random();
        
        var purposes = new[]
        {
            "Транспортні витрати", "Проживання", "Харчування", "Телефонні витрати", "Канцелярські товари",
            "Фото- та відеозйомка", "Експертиза", "Свідки", "Розшук інформації", "Технічне забезпечення",
            "Паливо", "Паркування", "Таксі", "Громадський транспорт", "Авіаквитки", "Залізничні квитки",
            "Готель", "Квартира", "Ресторан", "Кафе", "Продукти", "Мобільний зв'язок", "Інтернет",
            "Папір", "Ручки", "Блокноти", "Файли", "Папки", "Друк", "Копіювання", "Фотоапарат",
            "Відеокамера", "Диктофон", "Комп'ютер", "Принтер", "Сканер", "Лабораторія", "Аналізи",
            "Тести", "Дослідження", "Консультації", "Послуги", "Матеріали", "Обладнання"
        };
        
        var caseTitles = new[]
        {
            "Розслідування крадіжки", "Слідство по шахрайству", "Розшук зниклої особи", 
            "Розслідування підпалу", "Слідство по вимаганню", "Розшук злочинця",
            "Розслідування підробки", "Слідство по крадіжці", "Розшук свідків",
            "Розслідування зловживання", "Слідство по шантажу", "Розшук доказів"
        };
        
        var approvalStatuses = new[] { ApprovalStatus.Approved, ApprovalStatus.Pending, ApprovalStatus.Declined };
        
        for (int i = 1; i <= 40; i++)
        {
            var expenseDate = DateTime.Now.AddDays(-random.Next(1, 180)); // Last 6 months
            var amount = random.Next(100, 10000); // 100-10000 грн
            var approvalStatus = approvalStatuses[random.Next(approvalStatuses.Length)];
            
            sampleExpenses.Add(new ExpenseDto
            {
                Id = AllExpenses.Count + i,
                CaseId = random.Next(1, 100),
                CaseTitle = caseTitles[random.Next(caseTitles.Length)],
                Purpose = purposes[random.Next(purposes.Length)],
                Amount = amount,
                DateTime = expenseDate,
                ApprovalStatus = approvalStatus
            });
        }
        
        return sampleExpenses;
    }
}
