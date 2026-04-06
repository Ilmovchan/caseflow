using AutoMapper;
using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Dto.Suspect;
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
    public List<EvidenceCaseDto> Evidences { get; set; } = new();
    public List<SuspectDto> Suspects { get; set; } = new();
    public List<ReportDto> Reports { get; set; } = new();
    public List<ExpenseDto> Expenses { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _detectiveService.GetCaseAsync(id);
        if (entity == null) return NotFound();
        
        Case = _mapper.Map<CaseDto>(entity);

        // Connected entities
        Evidences = await _detectiveService.GetEvidencesFromCase(id);
        Suspects = await _detectiveService.GetSuspectsFromCase(id);
        Reports = await _detectiveService.GetReportsFromCaseAsync(id);
        Expenses = await _detectiveService.GetExpensesFromCaseAsync(id);

        return Page();
    }
}

