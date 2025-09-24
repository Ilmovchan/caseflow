using AutoMapper;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Reports;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<ReportDto> AllReports { get; set; } = new();
    public List<ReportDto> PendingReports { get; set; } = new();

    public async Task OnGetAsync()
    {
        var allReportEntities = await _adminService.GetReportsAsync();
        var pendingReportEntities = await _adminService.GetPendingReportsAsync();
        
        AllReports = _mapper.Map<List<ReportDto>>(allReportEntities);
        PendingReports = _mapper.Map<List<ReportDto>>(pendingReportEntities);
    }
}
