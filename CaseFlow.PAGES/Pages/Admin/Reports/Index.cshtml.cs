using AutoMapper;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace CaseFlow.PAGES.Pages.Admin.Reports;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<ReportDto> AllReports { get; set; } = new();
    public List<ReportDto> PendingReports { get; set; } = new();
    public PagedResult<ReportDto> PagedReports { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 15;

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;
        var pagedResult = await _adminService.GetReportsPagedAsync(pageNumber, PageSize);
        PagedReports = new PagedResult<ReportDto>
        {
            Items = _mapper.Map<List<ReportDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };

        AllReports = PagedReports.Items;

        // Get pending reports for approval section
        var pendingReportEntities = await _adminService.GetPendingReportsAsync();
        PendingReports = _mapper.Map<List<ReportDto>>(pendingReportEntities);
    }
    
    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        try
        {
            var approved = await _adminService.ApproveReportAsync(id);
            return new JsonResult(new { 
                success = true, 
                newStatus = "Approved", 
                newStatusText = "Схвалено", 
                newStatusColor = "success",
                approvalStatus = "Approved",
                report = approved 
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
            var rejected = await _adminService.RejectReportAsync(id);
            return new JsonResult(new { 
                success = true, 
                newStatus = "Declined", 
                newStatusText = "Відхилено", 
                newStatusColor = "danger",
                approvalStatus = "Declined",
                report = rejected 
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
            var reportDate = DateTime.Now.AddDays(-random.Next(1, 90)); // Last 3 months
            var approvalStatus = approvalStatuses[random.Next(approvalStatuses.Length)];
            
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
