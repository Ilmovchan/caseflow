using AutoMapper;
using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Cases;

[Authorize(Policy = "DetectiveOnly")]
public class DetailsModel(DetectiveService detectiveService, IMapper mapper) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;
    private readonly IMapper _mapper = mapper;

    public CaseDto Case { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _detectiveService.GetCaseAsync(id);
        if (entity == null) return NotFound();
        
        Case = _mapper.Map<CaseDto>(entity);
        return Page();
    }
}

