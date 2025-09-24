using AutoMapper;
using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Cases;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<CaseDto> Cases { get; set; } = new();

    public async Task OnGetAsync()
    {
        var caseEntities = await _adminService.GetCasesAsync();
        Cases = _mapper.Map<List<CaseDto>>(caseEntities);
    }
}


