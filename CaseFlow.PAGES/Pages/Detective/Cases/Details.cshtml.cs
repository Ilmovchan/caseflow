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

    public List<EvidenceCaseDto> LinkableEvidences { get; set; } = new();
    public List<SuspectDto> LinkableSuspects { get; set; } = new();
    public List<ReportDto> AssignableReports { get; set; } = new();
    public List<ExpenseDto> AssignableExpenses { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();

        var entity = await _detectiveService.GetCaseForDetectiveAsync(id, identity);
        if (entity == null) return NotFound();

        Case = _mapper.Map<CaseDto>(entity);

        Evidences = await _detectiveService.GetEvidencesFromCase(id, identity);
        Suspects = await _detectiveService.GetSuspectsFromCase(id, identity);
        Reports = await _detectiveService.GetReportsFromCaseAsync(id, identity);
        Expenses = await _detectiveService.GetExpensesFromCaseAsync(id, identity);

        var linkableE = await _detectiveService.GetEvidencesLinkableToCaseAsync(id, identity);
        var evIds = Evidences.Select(x => x.EvidenceId).ToHashSet();
        LinkableEvidences = linkableE.Where(x => !evIds.Contains(x.EvidenceId)).ToList();

        var linkableS = await _detectiveService.GetSuspectsLinkableToCaseAsync(id, identity);
        var sIds = Suspects.Select(x => x.Id).ToHashSet();
        LinkableSuspects = linkableS.Where(x => !sIds.Contains(x.Id)).ToList();

        AssignableReports = await _detectiveService.GetReportsAssignableToCaseAsync(id, identity);
        AssignableExpenses = await _detectiveService.GetExpensesAssignableToCaseAsync(id, identity);

        return Page();
    }

    public async Task<IActionResult> OnPostLinkEvidenceAsync(int id, int evidenceId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        await _detectiveService.LinkEvidenceToCaseAsync(evidenceId, id, identity);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUnlinkEvidenceAsync(int id, int evidenceId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        await _detectiveService.UnlinkEvidenceFromCaseAsync(evidenceId, id, identity);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostLinkSuspectAsync(int id, int suspectId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        await _detectiveService.LinkSuspectToCaseAsync(suspectId, id, identity);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUnlinkSuspectAsync(int id, int suspectId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        await _detectiveService.UnlinkSuspectFromCaseAsync(suspectId, id, identity);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAssignReportAsync(int id, int reportId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        await _detectiveService.AssignReportToCaseAsync(reportId, id, identity);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAssignExpenseAsync(int id, int expenseId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        await _detectiveService.AssignExpenseToCaseAsync(expenseId, id, identity);
        return RedirectToPage(new { id });
    }
}
