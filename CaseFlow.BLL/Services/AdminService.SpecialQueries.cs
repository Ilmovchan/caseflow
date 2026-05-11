using CaseFlow.BLL.Dto.AdminSpecial;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.BLL.Services;

public partial class AdminService
{
    private static string SqDetectivePib(Detective? d) =>
        d == null ? "—" : $"{d.LastName} {d.FirstName}" + (d.FatherName != null ? $" {d.FatherName}" : "");

    private static string SqClientPib(Client c) =>
        $"{c.LastName} {c.FirstName}" + (c.FatherName != null ? $" {c.FatherName}" : "");

    #region Спеціальні виборки (A17–A34)

    public async Task<List<AdminReportRowDto>> SqReportsForCaseAsync(int caseId)
    {
        var list = await context.Reports.AsNoTracking()
            .Include(r => r.CreatedByDetective)
            .Where(r => r.CaseId == caseId && r.ApprovalStatus != ApprovalStatus.Draft)
            .OrderBy(r => r.ReportDate).ThenBy(r => r.Id)
            .ToListAsync();

        return list.Select(r => new AdminReportRowDto
        {
            ReportId = r.Id,
            CaseId = r.CaseId,
            DetectiveId = r.CreatedByDetectiveId,
            DetectivePib = SqDetectivePib(r.CreatedByDetective),
            ReportDate = r.ReportDate,
            Summary = r.Summary,
            Comments = r.Comments
        }).ToList();
    }

    public async Task<List<Case>> SqCasesByStatusAsync(CaseStatus status) =>
        await context.Cases.AsNoTracking()
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .Include(c => c.CaseType)
            .Where(c => c.Status == status)
            .OrderBy(c => c.Id)
            .ToListAsync();

    public async Task<List<Case>> SqCasesNearestDeadlineAsync() =>
        await context.Cases.AsNoTracking()
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .Include(c => c.CaseType)
            .Where(c => c.Status != CaseStatus.Closed)
            .OrderBy(c => c.DeadlineDate).ThenBy(c => c.Id)
            .ToListAsync();

    public async Task<List<Case>> SqOpenCasesNearestDeadlineAsync() =>
        await context.Cases.AsNoTracking()
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .Include(c => c.CaseType)
            .Where(c => c.Status == CaseStatus.Opened)
            .OrderBy(c => c.DeadlineDate).ThenBy(c => c.Id)
            .ToListAsync();

    public async Task<List<Case>> SqCasesForClientAsync(int clientId) =>
        await context.Cases.AsNoTracking()
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .Include(c => c.CaseType)
            .Where(c => c.ClientId == clientId)
            .OrderBy(c => c.Id)
            .ToListAsync();

    public async Task<List<Client>> SqClientsRegisteredBetweenAsync(DateTime fromInclusive, DateTime toInclusive) =>
        await context.Clients.AsNoTracking()
            .Where(c => c.RegistrationDate >= fromInclusive && c.RegistrationDate <= toInclusive)
            .OrderBy(c => c.RegistrationDate).ThenBy(c => c.Id)
            .ToListAsync();

    public async Task<List<Case>> SqDetectiveWorkloadAsync(int detectiveId) =>
        await context.Cases.AsNoTracking()
            .Include(c => c.Client)
            .Include(c => c.CaseType)
            .Where(c => c.DetectiveId == detectiveId)
            .OrderBy(c => c.DeadlineDate).ThenBy(c => c.Id)
            .ToListAsync();

    public async Task<List<DetectiveClosedRankingRowDto>> SqDetectiveRankingByClosedCasesAsync()
    {
        var detectives = await context.Detectives.AsNoTracking().OrderBy(d => d.Id).ToListAsync();
        var cases = await context.Cases.AsNoTracking().ToListAsync();

        var ranked = detectives.Select(d =>
        {
            var dcases = cases.Where(c => c.DetectiveId == d.Id).ToList();
            var closed = dcases.Count(c => c.Status == CaseStatus.Closed);
            var total = dcases.Count;
            var pct = total == 0 ? 0 : Math.Round(100m * closed / total, 2);
            double? avgDays = null;
            var finished = dcases.Where(c => c.Status == CaseStatus.Closed && c.CloseDate.HasValue).ToList();
            if (finished.Count > 0)
                avgDays = finished.Average(c => (double)(c.CloseDate!.Value.DayNumber - c.StartDate.DayNumber));

            return new DetectiveClosedRankingRowDto
            {
                Place = 0,
                DetectiveId = d.Id,
                DetectivePib = SqDetectivePib(d),
                ClosedCasesCount = closed,
                SuccessPercent = pct,
                AvgCompletionDays = avgDays
            };
        }).OrderByDescending(r => r.ClosedCasesCount).ThenBy(r => r.DetectiveId).ToList();

        return ranked.Select((r, idx) => new DetectiveClosedRankingRowDto
        {
            Place = idx + 1,
            DetectiveId = r.DetectiveId,
            DetectivePib = r.DetectivePib,
            ClosedCasesCount = r.ClosedCasesCount,
            SuccessPercent = r.SuccessPercent,
            AvgCompletionDays = r.AvgCompletionDays
        }).ToList();
    }

    public async Task<List<Case>> SqOverdueDeadlineCasesAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await context.Cases.AsNoTracking()
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .Include(c => c.CaseType)
            .Where(c => c.Status != CaseStatus.Closed && c.DeadlineDate < today)
            .OrderBy(c => c.DeadlineDate).ThenBy(c => c.Id)
            .ToListAsync();
    }

    public async Task<List<ClientUnfinishedSummaryDto>> SqClientsWithUnfinishedCountsAsync()
    {
        var clients = await context.Clients.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
        var unfinished = await context.Cases.AsNoTracking()
            .Where(c => c.Status != CaseStatus.Closed)
            .GroupBy(c => c.ClientId)
            .Select(g => new { ClientId = g.Key, Cnt = g.Count() })
            .ToDictionaryAsync(x => x.ClientId, x => x.Cnt);

        return clients.Select(cl => new ClientUnfinishedSummaryDto
            {
                ClientId = cl.Id,
                ClientPib = SqClientPib(cl),
                UnfinishedCount = unfinished.GetValueOrDefault(cl.Id, 0)
            })
            .OrderByDescending(x => x.UnfinishedCount).ThenBy(x => x.ClientId)
            .ToList();
    }

    public async Task<(string DetectivePib, decimal? AverageCost)> SqAverageCaseCostAsync(int detectiveId)
    {
        var d = await context.Detectives.AsNoTracking().FirstOrDefaultAsync(x => x.Id == detectiveId);
        if (d == null)
            return ("—", null);

        var cases = await context.Cases.AsNoTracking()
            .Include(c => c.CaseType)
            .Where(c => c.DetectiveId == detectiveId)
            .ToListAsync();

        if (cases.Count == 0)
            return (SqDetectivePib(d), null);

        var totals = new List<decimal>();
        foreach (var c in cases)
        {
            var basePrice = c.CaseType?.Price ?? 0;
            var sumExp = await context.Expenses.Where(e => e.CaseId == c.Id && e.ApprovalStatus != ApprovalStatus.Draft).SumAsync(e => e.Amount);
            totals.Add(basePrice + sumExp);
        }

        return (SqDetectivePib(d), Math.Round(totals.Average(), 2));
    }

    public async Task<List<DetectiveYearRankingRowDto>> SqDetectiveYearRankingAsync(int year)
    {
        var detectives = await context.Detectives.AsNoTracking().ToListAsync();
        var casesYear = await context.Cases.AsNoTracking()
            .Include(c => c.CaseType)
            .Where(c => c.StartDate.Year == year)
            .ToListAsync();

        var caseIds = casesYear.Select(cs => cs.Id).ToList();

        var ce = await context.CaseEvidences.AsNoTracking()
            .Where(x => caseIds.Contains(x.CaseId))
            .ToListAsync();
        var csu = await context.CaseSuspects.AsNoTracking()
            .Where(x => caseIds.Contains(x.CaseId))
            .ToListAsync();

        var rows = new List<DetectiveYearRankingRowDto>();

        foreach (var d in detectives.OrderBy(x => x.Id))
        {
            var dc = casesYear.Where(c => c.DetectiveId == d.Id).ToList();
            var totalInYear = dc.Count;
            var closedInYear = dc.Count(c => c.Status == CaseStatus.Closed);
            var openIncomplete = dc.Count(c => c.Status != CaseStatus.Closed);

            var closedPct = totalInYear == 0
                ? 0
                : Math.Round(100m * closedInYear / totalInYear, 2);

            decimal totalValue = 0;
            foreach (var c in dc)
            {
                var basePrice = c.CaseType?.Price ?? 0;
                var sumExp = await context.Expenses.Where(e => e.CaseId == c.Id && e.ApprovalStatus != ApprovalStatus.Draft).SumAsync(e => e.Amount);
                totalValue += basePrice + sumExp;
            }

            var dcCaseIds = dc.Select(x => x.Id).ToHashSet();
            var evCount = ce.Where(x => dcCaseIds.Contains(x.CaseId)).Select(x => x.EvidenceId).Distinct().Count();
            var suCount = csu.Where(x => dcCaseIds.Contains(x.CaseId)).Select(x => x.SuspectId).Distinct().Count();

            rows.Add(new DetectiveYearRankingRowDto
            {
                DetectiveId = d.Id,
                DetectivePib = SqDetectivePib(d),
                ClosedPercent = closedPct,
                OpenIncompleteCount = openIncomplete,
                TotalCasesValue = Math.Round(totalValue, 2),
                EvidenceCount = evCount,
                SuspectsCount = suCount
            });
        }

        return rows.OrderByDescending(r => r.ClosedPercent).ThenBy(r => r.DetectiveId).ToList();
    }

    public async Task<List<FirstTimeClientRowDto>> SqFirstTimeClientsCurrentYearAsync(int year)
    {
        var yearStart = new DateOnly(year, 1, 1);
        var groups = await context.Cases.AsNoTracking()
            .GroupBy(c => c.ClientId)
            .Select(g => new { ClientId = g.Key, FirstStart = g.Min(x => x.StartDate) })
            .Where(x => x.FirstStart >= yearStart)
            .ToListAsync();

        var clientIdsInGroups = groups.Select(g => g.ClientId).ToList();
        var clients = await context.Clients.AsNoTracking()
            .Where(c => clientIdsInGroups.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        return groups
            .OrderBy(x => x.FirstStart).ThenBy(x => x.ClientId)
            .Select(x =>
            {
                var cl = clients[x.ClientId];
                return new FirstTimeClientRowDto
                {
                    ClientId = x.ClientId,
                    ClientPib = SqClientPib(cl),
                    FirstCaseDate = x.FirstStart
                };
            })
            .ToList();
    }

    public async Task<List<CaseTypeRankRowDto>> SqCaseTypePopularityRankingAsync()
    {
        var types = await context.CaseTypes.AsNoTracking().ToListAsync();
        var counts = await context.Cases.AsNoTracking()
            .GroupBy(c => c.CaseTypeId)
            .Select(g => new { TypeId = g.Key, Cnt = g.Count() })
            .ToDictionaryAsync(x => x.TypeId, x => x.Cnt);

        var sorted = types
            .Select(ct => new CaseTypeRankRowDto
            {
                Place = 0,
                CaseTypeName = ct.Name,
                Count = counts.GetValueOrDefault(ct.Id, 0)
            })
            .OrderByDescending(r => r.Count).ThenBy(r => r.CaseTypeName)
            .ToList();

        return sorted.Select((r, i) => new CaseTypeRankRowDto
        {
            Place = i + 1,
            CaseTypeName = r.CaseTypeName,
            Count = r.Count
        }).ToList();
    }

    public async Task<List<ClientUnfinishedRankRowDto>> SqClientUnfinishedRankingAsync()
    {
        var summaries = await SqClientsWithUnfinishedCountsAsync();
        var filtered = summaries.Where(s => s.UnfinishedCount > 0).ToList();
        var rows = filtered.Select((s, idx) => new ClientUnfinishedRankRowDto
            {
                Place = idx + 1,
                ClientId = s.ClientId,
                ClientPib = s.ClientPib,
                UnfinishedCount = s.UnfinishedCount
            })
            .ToList();
        return rows;
    }

    public async Task<List<CaseTypeRankRowDto>> SqCaseTypeUnfinishedRankingAsync()
    {
        var types = await context.CaseTypes.AsNoTracking().ToListAsync();
        var counts = await context.Cases.AsNoTracking()
            .Where(c => c.Status != CaseStatus.Closed)
            .GroupBy(c => c.CaseTypeId)
            .Select(g => new { TypeId = g.Key, Cnt = g.Count() })
            .ToDictionaryAsync(x => x.TypeId, x => x.Cnt);

        var sorted = types
            .Select(ct => new CaseTypeRankRowDto
            {
                Place = 0,
                CaseTypeName = ct.Name,
                Count = counts.GetValueOrDefault(ct.Id, 0)
            })
            .OrderByDescending(r => r.Count).ThenBy(r => r.CaseTypeName)
            .ToList();

        return sorted.Select((r, i) => new CaseTypeRankRowDto
        {
            Place = i + 1,
            CaseTypeName = r.CaseTypeName,
            Count = r.Count
        }).ToList();
    }

    public async Task<List<CaseExpenseRankRowDto>> SqCasesByExpenseCountRankingAsync()
    {
        var cases = await context.Cases.AsNoTracking()
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .ToListAsync();

        var expenseCounts = await context.Expenses.AsNoTracking()
            .Where(e => e.ApprovalStatus != ApprovalStatus.Draft)
            .GroupBy(e => e.CaseId)
            .Select(g => new { CaseId = g.Key, Cnt = g.Count() })
            .ToDictionaryAsync(x => x.CaseId, x => x.Cnt);

        var sorted = cases
            .Select(c => new CaseExpenseRankRowDto
            {
                Place = 0,
                CaseId = c.Id,
                CaseTitle = c.Title,
                ClientPib = SqClientPib(c.Client!),
                DetectivePib = SqDetectivePib(c.Detective),
                ExpensesCount = expenseCounts.GetValueOrDefault(c.Id, 0)
            })
            .OrderByDescending(r => r.ExpensesCount).ThenBy(r => r.CaseId)
            .ToList();

        return sorted.Select((r, i) => new CaseExpenseRankRowDto
        {
            Place = i + 1,
            CaseId = r.CaseId,
            CaseTitle = r.CaseTitle,
            ClientPib = r.ClientPib,
            DetectivePib = r.DetectivePib,
            ExpensesCount = r.ExpensesCount
        }).ToList();
    }

    #endregion

    #region Подання PostgreSQL (detective_ranking, first_time_clients)

    public async Task<List<DetectiveRankingViewRowDto>> SqlDetectiveRankingViewAsync() =>
        await context.Database
            .SqlQuery<DetectiveRankingViewRowDto>($"SELECT * FROM public.detective_ranking ORDER BY detective_id")
            .ToListAsync();

    public async Task<List<FirstTimeClientViewRowDto>> SqlFirstTimeClientsViewAsync() =>
        await context.Database
            .SqlQuery<FirstTimeClientViewRowDto>($"SELECT * FROM public.first_time_clients ORDER BY client_id")
            .ToListAsync();

    #endregion
}
