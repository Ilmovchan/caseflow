using AutoMapper;
using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.DetectiveSpecial;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using CaseFlow.PAGES.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.SpecialQueries;

[Authorize(Policy = "DetectiveOnly")]
public class IndexModel(DetectiveService detectiveService, IMapper mapper) : PageModel
{
    private readonly DetectiveService _svc = detectiveService;
    private readonly IMapper _mapper = mapper;

    public List<(int Id, string Name)> CaseTypeOptions { get; set; } = [];

    public string? FlashSuccess { get; set; }
    public string? FlashError { get; set; }

    public List<CaseDto>? CasesByTypeRows { get; set; }
    public List<CaseDto>? UnclosedCasesRows { get; set; }
    public List<SuspectDto>? PhysicalRows { get; set; }
    public ReportDto? LatestReportRow { get; set; }
    public bool ShowLatestReportEmpty { get; set; }
    public List<SuspectDto>? LocationRows { get; set; }
    public List<SuspectLinkedCaseRowDto>? SuspectCasesRows { get; set; }

    public async Task OnGetAsync()
    {
        await LoadCaseTypesAsync();
    }

    private async Task<string?> LoadCaseTypesAsync()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return null;
        CaseTypeOptions = await _svc.GetCaseTypesUsedByDetectiveCasesAsync(identity);
        return identity;
    }

    public async Task<IActionResult> OnPostCloseCaseAsync(int caseId)
    {
        var identity = await LoadCaseTypesAsync();
        if (identity == null)
            return Unauthorized();

        try
        {
            await _svc.CloseCaseAsync(caseId, identity);
            FlashSuccess = $"Справа #{caseId} закрита.";
        }
        catch (EntityNotFoundException ex)
        {
            FlashError = ex.Message;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCasesByTypeAsync(int caseTypeId)
    {
        var identity = await LoadCaseTypesAsync();
        if (identity == null)
            return Unauthorized();

        try
        {
            var cases = await _svc.GetCasesForDetectiveByCaseTypeAsync(caseTypeId, identity);
            CasesByTypeRows = _mapper.Map<List<CaseDto>>(cases);
        }
        catch (EntityNotFoundException ex)
        {
            FlashError = ex.Message;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostUnclosedAsync()
    {
        var identity = await LoadCaseTypesAsync();
        if (identity == null)
            return Unauthorized();

        var cases = await _svc.GetUnclosedCasesForDetectiveAsync(identity);
        UnclosedCasesRows = _mapper.Map<List<CaseDto>>(cases);
        return Page();
    }

    public async Task<IActionResult> OnPostPhysicalAsync(
        int? heightMin,
        int? heightMax,
        int? weightMin,
        int? weightMax,
        string? physicalDescriptionContains)
    {
        var identity = await LoadCaseTypesAsync();
        if (identity == null)
            return Unauthorized();

        var hasAny =
            heightMin.HasValue || heightMax.HasValue || weightMin.HasValue || weightMax.HasValue
            || !string.IsNullOrWhiteSpace(physicalDescriptionContains);
        if (!hasAny)
        {
            FlashError = "Вкажіть хоча б один критерій (зріст, вагу або фрагмент опису).";
            return Page();
        }

        PhysicalRows = await _svc.SearchSuspectsByPhysicalCharacteristicsAsync(
            identity,
            heightMin,
            heightMax,
            weightMin,
            weightMax,
            physicalDescriptionContains);
        return Page();
    }

    public async Task<IActionResult> OnPostLatestReportAsync(int caseId)
    {
        var identity = await LoadCaseTypesAsync();
        if (identity == null)
            return Unauthorized();

        try
        {
            LatestReportRow = await _svc.GetLatestReportForCaseAsync(caseId, identity);
            ShowLatestReportEmpty = LatestReportRow == null;
        }
        catch (EntityNotFoundException ex)
        {
            FlashError = ex.Message;
            ShowLatestReportEmpty = false;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostLocationAsync(string? city, string? region)
    {
        var identity = await LoadCaseTypesAsync();
        if (identity == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(region))
        {
            FlashError = "Вкажіть місто або регіон.";
            return Page();
        }

        LocationRows = await _svc.SearchSuspectsByLocationAsync(identity, city, region);
        return Page();
    }

    public async Task<IActionResult> OnPostSuspectCasesAsync(int suspectId)
    {
        var identity = await LoadCaseTypesAsync();
        if (identity == null)
            return Unauthorized();

        SuspectCasesRows = await _svc.GetCasesLinkedToSuspectAsync(suspectId, identity);
        return Page();
    }
}
