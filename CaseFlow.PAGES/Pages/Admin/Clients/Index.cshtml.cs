using AutoMapper;
using CaseFlow.BLL.Dto.Client;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Clients;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<ClientDto> Clients { get; set; } = new();
    public PagedResult<ClientDto> PagedClients { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 15;

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;
        var pagedResult = await _adminService.GetClientsPagedAsync(pageNumber, PageSize);
        PagedClients = new PagedResult<ClientDto>
        {
            Items = _mapper.Map<List<ClientDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };

        Clients = PagedClients.Items;
    }

}


