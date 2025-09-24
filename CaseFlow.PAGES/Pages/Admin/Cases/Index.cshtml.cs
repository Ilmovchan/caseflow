using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Cases;

public class IndexModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public List<Case> Cases { get; set; } = new();

    public async Task OnGetAsync()
    {
        Cases = await _adminService.GetCasesAsync();
    }
}


