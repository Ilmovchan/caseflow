using AutoMapper;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Services;
using CaseFlow.PAGES.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CaseFlow.PAGES.Pages.Detective.Evidence;

[Authorize(Policy = "DetectiveOnly")]
[IgnoreAntiforgeryToken]
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

        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
        {
            Evidences = [];
            return;
        }

        var allEvidences = await _detectiveService.GetEvidencesAsync(identity);
        
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            allEvidences = allEvidences.Where(e =>
                e.EvidenceId.ToString().Contains(term) ||
                (e.CaseId?.ToString() ?? "").Contains(term) ||
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

    public async Task<IActionResult> OnPostApproveAsync([FromQuery] int id)
    {
        try
        {
            var identity = DetectiveIdentity.FromUser(User);
            if (string.IsNullOrEmpty(identity))
                return Unauthorized();
            var submitted = await _detectiveService.SubmitEvidenceAsync(id, identity);
            return new JsonResult(new {
                success = true,
                newStatus = "Pending",
                newStatusText = "Очікує",
                newStatusColor = "warning",
                approvalStatus = "Pending",
                message = "Доказ надіслано на перевірку!",
                evidence = submitted
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new {
                success = false,
                error = ex.Message
            });
        }
    }

    public async Task<IActionResult> OnPostRejectAsync([FromQuery] int id)
    {
        try
        {
            var identity = DetectiveIdentity.FromUser(User);
            if (string.IsNullOrEmpty(identity))
                return Unauthorized();
            await _detectiveService.DeleteEvidenceAsync(id, identity);
            return new JsonResult(new {
                success = true,
                message = "Доказ видалено успішно"
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new {
                success = false,
                error = ex.Message
            });
        }
    }
}
