using AutoMapper;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CaseFlow.PAGES.Pages.Admin.Reports;

[IgnoreAntiforgeryToken]
public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<ReportDto> AllReports { get; set; } = new();
    public List<ReportDto> PendingReports { get; set; } = new();
    public PagedResult<ReportDto> PagedReports { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;

        List<Report> allReports;
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchResult = await _adminService.SearchReportsPagedAsync(SearchTerm, 1, int.MaxValue);
            allReports = searchResult.Items;
        }
        else
        {
            var allResult = await _adminService.GetReportsPagedAsync(1, int.MaxValue);
            allReports = allResult.Items;
        }

        var totalCount = allReports.Count;
        var items = allReports
            .OrderBy(r => r.Id)
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        PagedReports = new PagedResult<ReportDto>
        {
            Items = _mapper.Map<List<ReportDto>>(items),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = PageSize
        };

        AllReports = PagedReports.Items;

        var pendingReportEntities = await _adminService.GetPendingReportsAsync();
        PendingReports = _mapper.Map<List<ReportDto>>(pendingReportEntities);
    }
    
    public async Task<IActionResult> OnPostApproveAsync([FromQuery] int id)
    {
        try
        {
            var approved = await _adminService.ApproveReportAsync(id);
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
            var rejected = await _adminService.RejectReportAsync(id);
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
            var submitted = await _adminService.SubmitReportAsync(id);
            return new JsonResult(new {
                success = true,
                newStatus = "Pending",
                newStatusText = "Очікує",
                newStatusColor = "warning",
                approvalStatus = "Pending",
                message = "Звіт надіслано на перевірку!",
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
            await _adminService.DeleteReportAsync(id);
            return new JsonResult(new {
                success = true,
                message = "Звіт видалено успішно"
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
    
    private List<ReportDto> GenerateSampleReports()
    {
        var sampleReports = new List<ReportDto>();
        var random = new Random();
        
        var caseTitles = new[]
        {
            "Розслідування крадіжки", "Слідство по шахрайству", "Розшук зниклої особи", 
            "Розслідування підпалу", "Слідство по вимаганню", "Розшук злочинця",
            "Розслідування підробки", "Слідство по крадіжці", "Розшук свідків",
            "Розслідування зловживання", "Слідство по шантажу", "Розшук доказів"
        };
        
        var summaries = new[]
        {
            "Проведено повне розслідування справи", "Зібрано всі необхідні докази", "Опітано всіх свідків",
            "Проведено експертизу", "Виконано всі необхідні дії", "Завершено розслідування",
            "Підтверджено підозри", "Знайдено докази вини", "Встановлено обставини",
            "Виявлено порушення", "Встановлено факт злочину", "Підтверджено алібі",
            "Знайдено свідків", "Отримано важливі свідчення", "Проведено аналіз",
            "Виконано всі завдання", "Досягнуто мети розслідування", "Завершено справу",
            "Підтверджено невинність", "Встановлено справжнього злочинця", "Розкрито злочин",
            "Знайдено зниклу особу", "Повернуто вкрадене", "Відновлено справедливість",
            "Захищено права потерпілого", "Покарано злочинця", "Попереджено повторні злочини"
        };
        
        var approvalStatuses = new[] { ApprovalStatus.Approved, ApprovalStatus.Pending, ApprovalStatus.Declined };
        
        for (int i = 1; i <= 50; i++)
        {
            var reportDate = DateTime.Now.AddDays(-random.Next(1, 90));            var approvalStatus = approvalStatuses[random.Next(approvalStatuses.Length)];
            
            sampleReports.Add(new ReportDto
            {
                Id = AllReports.Count + i,
                CaseId = random.Next(1, 100),
                CaseTitle = caseTitles[random.Next(caseTitles.Length)],
                Summary = summaries[random.Next(summaries.Length)],
                ReportDate = reportDate,
                ApprovalStatus = approvalStatus
            });
        }
        
        return sampleReports;
    }
}
