using AutoMapper;
using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Services;
using CaseFlow.PAGES.Extensions;
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
    public List<EvidenceCaseDto> Evidences { get; set; } = new();
    public List<SuspectDto> Suspects { get; set; } = new();
    public List<ReportDto> Reports { get; set; } = new();
    public List<ExpenseDto> Expenses { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();

        var entity = await _detectiveService.GetCaseForDetectiveAsync(id, identity);
        if (entity == null) return NotFound();
        
        Case = _mapper.Map<CaseDto>(entity);

        // Connected entities
        Evidences = await _detectiveService.GetEvidencesFromCase(id, identity);
        Suspects = await _detectiveService.GetSuspectsFromCase(id, identity);
        Reports = await _detectiveService.GetReportsFromCaseAsync(id, identity);
        Expenses = await _detectiveService.GetExpensesFromCaseAsync(id, identity);

        return Page();
    }
}

