using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Evidence;

[Authorize(Policy = "DetectiveOnly")]
public class DetailsModel(DetectiveService detectiveService) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;

    public EvidenceCaseDto Evidence { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _detectiveService.GetEvidenceAsync(id);
        if (entity == null) return NotFound();
        Evidence = entity;
        return Page();
    }
}

