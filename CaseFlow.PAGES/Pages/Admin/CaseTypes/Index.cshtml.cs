using AutoMapper;
using CaseFlow.BLL.Dto.CaseType;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.CaseTypes;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<CaseTypeDto> CaseTypes { get; set; } = new();

    public async Task OnGetAsync()
    {
        var caseTypeEntities = await _adminService.GetCaseTypesAsync();
        CaseTypes = _mapper.Map<List<CaseTypeDto>>(caseTypeEntities);
    }
}
