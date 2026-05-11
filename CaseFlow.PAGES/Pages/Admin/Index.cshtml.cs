using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin;

[Authorize(Policy = "AdminOnly")]
public class IndexModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public string UserFullName { get; set; } = string.Empty;
    public int PendingEvidenceCount { get; set; }
    public int PendingSuspectsCount { get; set; }
    public int PendingExpensesCount { get; set; }
    public int PendingReportsCount { get; set; }

    public async Task OnGetAsync()
    {
        UserFullName = User.Identity?.Name ?? "Адміністратор";
        
        var pendingEvidence = await _adminService.GetPendingEvidencesAsync();
        PendingEvidenceCount = pendingEvidence.Count;
        
        var pendingSuspects = await _adminService.GetPendingSuspectsAsync();
        PendingSuspectsCount = pendingSuspects.Count;
        
        var pendingExpenses = await _adminService.GetPendingExpensesAsync();
        PendingExpensesCount = pendingExpenses.Count;
        
        var pendingReports = await _adminService.GetPendingReportsAsync();
        PendingReportsCount = pendingReports.Count;
    }
}
