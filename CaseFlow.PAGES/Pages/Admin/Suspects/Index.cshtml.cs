using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Suspects;

public class IndexModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public List<Suspect> AllSuspects { get; set; } = new();
    public List<Suspect> PendingSuspects { get; set; } = new();

    public async Task OnGetAsync()
    {
        AllSuspects = await _adminService.GetSuspectsAsync();
        PendingSuspects = await _adminService.GetPendingSuspectsAsync();
    }
}
