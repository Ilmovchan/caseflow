using AutoMapper;
using CaseFlow.BLL.Dto.Detective;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Detectives;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<DetectiveDto> Detectives { get; set; } = new();

    public async Task OnGetAsync()
    {
        var detectiveEntities = await _adminService.GetDetectivesAsync();
        Detectives = _mapper.Map<List<DetectiveDto>>(detectiveEntities);
    }
}


