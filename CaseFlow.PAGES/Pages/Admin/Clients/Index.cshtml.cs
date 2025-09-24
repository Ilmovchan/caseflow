using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Clients;

public class IndexModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public List<Client> Clients { get; set; } = new();

    public async Task OnGetAsync()
    {
        Clients = await _adminService.GetClientsAsync();
    }
}


