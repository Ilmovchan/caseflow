using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Detectives;

public class IndexModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public List<Detective> Detectives { get; set; } = new();

    public async Task OnGetAsync()
    {
        Detectives = await _adminService.GetDetectivesAsync();
    }
}


