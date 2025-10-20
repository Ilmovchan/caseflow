using AutoMapper;
using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace CaseFlow.PAGES.Pages.Admin.Expenses;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<ExpenseDto> AllExpenses { get; set; } = new();
    public List<ExpenseDto> PendingExpenses { get; set; } = new();
    public PagedResult<ExpenseDto> PagedExpenses { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 15;

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;
        var pagedResult = await _adminService.GetExpensesPagedAsync(pageNumber, PageSize);
        PagedExpenses = new PagedResult<ExpenseDto>
        {
            Items = _mapper.Map<List<ExpenseDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };
        
        AllExpenses = PagedExpenses.Items;
        
        // Add sample data if we have less than 10 expenses
        if (AllExpenses.Count < 10)
        {
            var sampleExpenses = GenerateSampleExpenses();
            AllExpenses.AddRange(sampleExpenses);
            // Re-sort the combined list to maintain ID order
            AllExpenses = AllExpenses.OrderBy(e => e.Id).ToList();
            PendingExpenses.AddRange(sampleExpenses.Where(e => e.ApprovalStatus.ToString() == "Pending"));
        }
        
        // Get pending expenses for approval section
        var pendingExpenseEntities = await _adminService.GetPendingExpensesAsync();
        PendingExpenses = _mapper.Map<List<ExpenseDto>>(pendingExpenseEntities);
    }
    
    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        try
        {
            var approved = await _adminService.ApproveExpenseAsync(id);
            return new JsonResult(new { 
                success = true, 
                newStatus = "Approved", 
                newStatusText = "Схвалено", 
                newStatusColor = "success",
                approvalStatus = "Approved",
                expense = approved 
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
    
    public async Task<IActionResult> OnPostRejectAsync(int id)
    {
        try
        {
            var rejected = await _adminService.RejectExpenseAsync(id);
            return new JsonResult(new { 
                success = true, 
                newStatus = "Declined", 
                newStatusText = "Відхилено", 
                newStatusColor = "danger",
                approvalStatus = "Declined",
                expense = rejected 
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
