using AutoMapper;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Evidence;

[Authorize(Policy = "DetectiveOnly")]
public class IndexModel(DetectiveService detectiveService, IMapper mapper) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;
    private readonly IMapper _mapper = mapper;

    public List<EvidenceCaseDto> Evidences { get; set; } = new();
    public PagedResult<EvidenceCaseDto> PagedEvidences { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;

        var allEvidences = await _detectiveService.GetEvidencesAsync();
        
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            allEvidences = allEvidences.Where(e =>
                e.EvidenceId.ToString().Contains(term) ||
                e.CaseId.ToString().Contains(term) ||
                e.Type.ToString().ToLower().Contains(term) ||
                (!string.IsNullOrEmpty(e.Description) && e.Description.ToLower().Contains(term)) ||
                e.CollectionDate.ToString().Contains(term) ||
                (!string.IsNullOrEmpty(e.Region) && e.Region.ToLower().Contains(term)))
                .ToList();
        }

        var totalCount = allEvidences.Count;
        var items = allEvidences
            .OrderBy(e => e.EvidenceId)
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        PagedEvidences = new PagedResult<EvidenceCaseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = PageSize
        };

        Evidences = PagedEvidences.Items;
    }
}

