using AutoMapper;
using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Evidence;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<EvidenceDto> AllEvidence { get; set; } = new();
    public List<EvidenceDto> PendingEvidence { get; set; } = new();

    public async Task OnGetAsync()
    {
        var allEvidenceEntities = await _adminService.GetEvidencesAsync();
        var pendingEvidenceEntities = await _adminService.GetPendingEvidencesAsync();
        
        AllEvidence = _mapper.Map<List<EvidenceDto>>(allEvidenceEntities);
        PendingEvidence = _mapper.Map<List<EvidenceDto>>(pendingEvidenceEntities);
    }
}
