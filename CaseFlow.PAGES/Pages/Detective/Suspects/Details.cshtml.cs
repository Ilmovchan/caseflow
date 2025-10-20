using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Suspects;

[Authorize(Policy = "DetectiveOnly")]
public class DetailsModel(DetectiveService detectiveService) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;

    public SuspectDto Suspect { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _detectiveService.GetSuspectAsync(id);
        if (entity == null) return NotFound();
        Suspect = entity;
        return Page();
    }
}

