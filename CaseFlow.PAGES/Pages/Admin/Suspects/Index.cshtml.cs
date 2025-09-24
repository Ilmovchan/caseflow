using AutoMapper;
using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Suspects;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<SuspectDto> AllSuspects { get; set; } = new();
    public List<SuspectDto> PendingSuspects { get; set; } = new();

    public async Task OnGetAsync()
    {
        var allSuspectEntities = await _adminService.GetSuspectsAsync();
        var pendingSuspectEntities = await _adminService.GetPendingSuspectsAsync();
        
        AllSuspects = _mapper.Map<List<SuspectDto>>(allSuspectEntities);
        PendingSuspects = _mapper.Map<List<SuspectDto>>(pendingSuspectEntities);
    }
}
