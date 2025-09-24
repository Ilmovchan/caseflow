using AutoMapper;
using CaseFlow.BLL.Dto.Client;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Clients;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<ClientDto> Clients { get; set; } = new();

    public async Task OnGetAsync()
    {
        var clientEntities = await _adminService.GetClientsAsync();
        Clients = _mapper.Map<List<ClientDto>>(clientEntities);
    }
}


