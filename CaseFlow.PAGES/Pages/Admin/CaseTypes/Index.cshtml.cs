using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.CaseTypes;

public class IndexModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public List<CaseType> CaseTypes { get; set; } = new();

    public async Task OnGetAsync()
    {
        CaseTypes = await _adminService.GetCaseTypesAsync();
    }
}
