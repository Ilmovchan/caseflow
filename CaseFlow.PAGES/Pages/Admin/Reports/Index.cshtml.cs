using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Reports;

public class IndexModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public List<Report> AllReports { get; set; } = new();
    public List<Report> PendingReports { get; set; } = new();

    public async Task OnGetAsync()
    {
        AllReports = await _adminService.GetReportsAsync();
        PendingReports = await _adminService.GetPendingReportsAsync();
    }
}
