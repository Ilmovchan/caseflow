using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Evidence;

public class IndexModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public List<DAL.Models.Evidence> AllEvidence { get; set; } = new();
    public List<DAL.Models.Evidence> PendingEvidence { get; set; } = new();

    public async Task OnGetAsync()
    {
        AllEvidence = await _adminService.GetEvidencesAsync();
        PendingEvidence = await _adminService.GetPendingEvidencesAsync();
    }
}
