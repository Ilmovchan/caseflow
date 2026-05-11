using AutoMapper;
using CaseFlow.BLL.Dto.AdminSpecial;
using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.Client;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.SpecialQueries;

[Authorize(Policy = "AdminOnly")]
public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<AdminReportRowDto>? ReportsCaseRows { get; set; }
    public List<CaseDto>? CasesStatusRows { get; set; }
    public List<CaseDto>? NearestDeadlineRows { get; set; }
    public List<CaseDto>? OpenNearestDeadlineRows { get; set; }
    public List<CaseDto>? CasesClientRows { get; set; }
    public List<ClientDto>? ClientsRegRows { get; set; }
    public List<CaseDto>? WorkloadRows { get; set; }
    public List<DetectiveClosedRankingRowDto>? RankingClosedRows { get; set; }
    public List<CaseDto>? OverdueRows { get; set; }
    public List<ClientUnfinishedSummaryDto>? ClientUnfinishedRows { get; set; }
    public string? AvgCostDetectivePib { get; set; }
    public decimal? AvgCostValue { get; set; }
    public List<DetectiveYearRankingRowDto>? YearRankingRows { get; set; }
    public List<FirstTimeClientRowDto>? FirstTimeClientRows { get; set; }
    public List<CaseTypeRankRowDto>? CaseTypePopularityRows { get; set; }
    public List<ClientUnfinishedRankRowDto>? ClientUnfinishedRankRows { get; set; }
    public List<CaseTypeRankRowDto>? CaseTypeUnfinishedRows { get; set; }
    public List<CaseExpenseRankRowDto>? CaseExpenseRankRows { get; set; }
    public ReportDto? SingleReport { get; set; }

    public string? SqlErrorMessage { get; set; }
    public List<DetectiveRankingViewRowDto>? SqlDetectiveRankingRows { get; set; }
    public List<FirstTimeClientViewRowDto>? SqlFirstTimeClientsViewRows { get; set; }

    public Task OnGetAsync() => Task.CompletedTask;

    public async Task<IActionResult> OnPostSqlDetectiveRankingAsync()
    {
        ClearSqlSectionState();
        try
        {
            SqlDetectiveRankingRows = await _adminService.SqlDetectiveRankingViewAsync();
        }
        catch (Exception ex)
        {
            SqlErrorMessage = ex.InnerException?.Message ?? ex.Message;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSqlFirstTimeClientsViewAsync()
    {
        ClearSqlSectionState();
        try
        {
            SqlFirstTimeClientsViewRows = await _adminService.SqlFirstTimeClientsViewAsync();
        }
        catch (Exception ex)
        {
            SqlErrorMessage = ex.InnerException?.Message ?? ex.Message;
        }

        return Page();
    }

    private void ClearSqlSectionState()
    {
        SqlErrorMessage = null;
        SqlDetectiveRankingRows = null;
        SqlFirstTimeClientsViewRows = null;
    }

    public async Task<IActionResult> OnPostReportsCaseAsync(int caseId)
    {
        ReportsCaseRows = await _adminService.SqReportsForCaseAsync(caseId);
        return Page();
    }

    public async Task<IActionResult> OnPostCasesStatusAsync(CaseStatus statusFilter)
    {
        var cases = await _adminService.SqCasesByStatusAsync(statusFilter);
        CasesStatusRows = _mapper.Map<List<CaseDto>>(cases);
        return Page();
    }

    public async Task<IActionResult> OnPostNearestDeadlineAsync()
    {
        var cases = await _adminService.SqCasesNearestDeadlineAsync();
        NearestDeadlineRows = _mapper.Map<List<CaseDto>>(cases);
        return Page();
    }

    public async Task<IActionResult> OnPostOpenNearestDeadlineAsync()
    {
        var cases = await _adminService.SqOpenCasesNearestDeadlineAsync();
        OpenNearestDeadlineRows = _mapper.Map<List<CaseDto>>(cases);
        return Page();
    }

    public async Task<IActionResult> OnPostCasesClientAsync(int clientId)
    {
        var cases = await _adminService.SqCasesForClientAsync(clientId);
        CasesClientRows = _mapper.Map<List<CaseDto>>(cases);
        return Page();
    }

    public async Task<IActionResult> OnPostClientsRegAsync(DateOnly regFrom, DateOnly regTo)
    {
        var fromUtc = DateTime.SpecifyKind(regFrom.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(regTo.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
        var clients = await _adminService.SqClientsRegisteredBetweenAsync(fromUtc, toUtc);
        ClientsRegRows = _mapper.Map<List<ClientDto>>(clients);
        return Page();
    }

    public async Task<IActionResult> OnPostWorkloadAsync(int detectiveId)
    {
        var cases = await _adminService.SqDetectiveWorkloadAsync(detectiveId);
        WorkloadRows = _mapper.Map<List<CaseDto>>(cases);
        return Page();
    }

    public async Task<IActionResult> OnPostRankingClosedAsync()
    {
        RankingClosedRows = await _adminService.SqDetectiveRankingByClosedCasesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostOverdueAsync()
    {
        var cases = await _adminService.SqOverdueDeadlineCasesAsync();
        OverdueRows = _mapper.Map<List<CaseDto>>(cases);
        return Page();
    }

    public async Task<IActionResult> OnPostClientUnfinishedAsync()
    {
        ClientUnfinishedRows = await _adminService.SqClientsWithUnfinishedCountsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAvgCostAsync(int detectiveIdCost)
    {
        var (pib, avg) = await _adminService.SqAverageCaseCostAsync(detectiveIdCost);
        AvgCostDetectivePib = pib;
        AvgCostValue = avg;
        return Page();
    }

    public async Task<IActionResult> OnPostYearRankingAsync(int rankingYear)
    {
        YearRankingRows = await _adminService.SqDetectiveYearRankingAsync(rankingYear);
        return Page();
    }

    public async Task<IActionResult> OnPostFirstTimeAsync(int firstTimeYear)
    {
        FirstTimeClientRows = await _adminService.SqFirstTimeClientsCurrentYearAsync(firstTimeYear);
        return Page();
    }

    public async Task<IActionResult> OnPostCaseTypePopularityAsync()
    {
        CaseTypePopularityRows = await _adminService.SqCaseTypePopularityRankingAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostClientUnfinishedRankAsync()
    {
        ClientUnfinishedRankRows = await _adminService.SqClientUnfinishedRankingAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCaseTypeUnfinishedRankAsync()
    {
        CaseTypeUnfinishedRows = await _adminService.SqCaseTypeUnfinishedRankingAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCaseExpenseRankAsync()
    {
        CaseExpenseRankRows = await _adminService.SqCasesByExpenseCountRankingAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostReportSingleAsync(int reportId)
    {
        var r = await _adminService.GetReportAsync(reportId);
        SingleReport = r == null ? null : _mapper.Map<ReportDto>(r);
        return Page();
    }
}
