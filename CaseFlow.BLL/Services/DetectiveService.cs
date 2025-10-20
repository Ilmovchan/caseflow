using AutoMapper;
using AutoMapper.QueryableExtensions;
using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Exceptions;
using CaseFlow.DAL.Data;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.BLL.Services;

public class DetectiveService(DetectiveAgencyDbContext context, IMapper mapper)
{
    #region Case
    public async Task<Case?> GetCaseAsync(int caseId) =>
        await context.Cases
            .Include(c => c.CaseType)
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .FirstOrDefaultAsync(c => c.Id == caseId);

    public async Task<List<Case>> GetCasesAsync() =>
        await context.Cases
            .Include(c => c.CaseType)
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .ToListAsync();

    public async Task<Case> UpdateCaseAsync(int caseId, UpdateCaseByDetectiveDto dto)
    {
        var caseEntity = await context.Cases.FindAsync(caseId)
            ?? throw new EntityNotFoundException("Case", caseId);

        mapper.Map(dto, caseEntity);
        await context.SaveChangesAsync();
        return caseEntity;
    }
    #endregion

    #region Client
    public async Task<Client?> GetClientAsync(int clientId) =>
        await context.Clients.FindAsync(clientId);

    public async Task<List<Client>> GetClientsAsync() =>
        await context.Clients.ToListAsync();
    #endregion

    #region Evidence
    public async Task<EvidenceCaseDto> CreateEvidenceAsync(int caseId, CreateEvidenceDto dto)
    {
        var evidenceEntity = mapper.Map<Evidence>(dto);
        context.Evidences.Add(evidenceEntity);

        var caseEvidenceEntity = new CaseEvidence
        {
            CaseId = caseId,
            Evidence = evidenceEntity,
            ApprovalStatus = ApprovalStatus.Draft,
        };

        context.CaseEvidences.Add(caseEvidenceEntity);
        await context.SaveChangesAsync();

        var result = mapper.Map<EvidenceCaseDto>(evidenceEntity);
        result.CaseId = caseId;
        return result;
    }

    public async Task<EvidenceCaseDto> UpdateEvidenceAsync(int evidenceId, UpdateEvidenceDto dto)
    {
        var evidence = await context.Evidences.FindAsync(evidenceId)
            ?? throw new EntityNotFoundException("Evidence", evidenceId);

        mapper.Map(dto, evidence);
        await context.SaveChangesAsync();

        var result = mapper.Map<EvidenceCaseDto>(evidence);
        result.EvidenceId = evidence.Id;
        return result;
    }

    public async Task DeleteEvidenceAsync(int evidenceId)
    {
        var evidence = await context.Evidences.FindAsync(evidenceId)
            ?? throw new EntityNotFoundException("Evidence", evidenceId);

        context.Evidences.Remove(evidence);
        await context.SaveChangesAsync();
    }

    public async Task<EvidenceCaseDto?> GetEvidenceAsync(int evidenceId) =>
        await context.CaseEvidences
            .Where(ce => ce.EvidenceId == evidenceId)
            .Select(ce => new EvidenceCaseDto
            {
                EvidenceId = ce.EvidenceId,
                CaseId = ce.CaseId,
                Type = ce.Evidence.Type,
                Description = ce.Evidence.Description,
                CollectionDate = ce.Evidence.CollectionDate,
                Region = ce.Evidence.Region,
                Annotation = ce.Evidence.Annotation,
                Purpose = ce.Evidence.Purpose
            })
            .FirstOrDefaultAsync();

    public async Task<List<EvidenceCaseDto>> GetEvidencesAsync() =>
        await context.CaseEvidences
            .Select(ce => new EvidenceCaseDto
            {
                EvidenceId = ce.EvidenceId,
                CaseId = ce.CaseId,
                Type = ce.Evidence.Type,
                Description = ce.Evidence.Description,
                CollectionDate = ce.Evidence.CollectionDate,
                Region = ce.Evidence.Region,
                Annotation = ce.Evidence.Annotation,
                Purpose = ce.Evidence.Purpose
            })
            .ToListAsync();

    public async Task<List<EvidenceCaseDto>> GetEvidencesFromCase(int caseId) =>
        await context.CaseEvidences
            .Where(ce => ce.CaseId == caseId)
            .Select(ce => new EvidenceCaseDto
            {
                EvidenceId = ce.EvidenceId,
                CaseId = ce.CaseId,
                Type = ce.Evidence.Type,
                Description = ce.Evidence.Description,
                CollectionDate = ce.Evidence.CollectionDate,
                Region = ce.Evidence.Region,
                Annotation = ce.Evidence.Annotation,
                Purpose = ce.Evidence.Purpose
            })
            .ToListAsync();

    public async Task<List<EvidenceCaseDto>> GetApprovedEvidencesAsync() =>
        await context.CaseEvidences
            .Where(ce => ce.ApprovalStatus == ApprovalStatus.Approved)
            .Select(ce => new EvidenceCaseDto
            {
                EvidenceId = ce.EvidenceId,
                CaseId = ce.CaseId,
                Type = ce.Evidence.Type,
                Description = ce.Evidence.Description,
                CollectionDate = ce.Evidence.CollectionDate,
                Region = ce.Evidence.Region,
                Annotation = ce.Evidence.Annotation,
                Purpose = ce.Evidence.Purpose
            })
            .ToListAsync();

    public async Task<List<EvidenceCaseDto>> GetDeclinedEvidencesAsync() =>
        await context.CaseEvidences
            .Where(ce => ce.ApprovalStatus == ApprovalStatus.Declined)
            .Select(ce => new EvidenceCaseDto
            {
                EvidenceId = ce.EvidenceId,
                CaseId = ce.CaseId,
                Type = ce.Evidence.Type,
                Description = ce.Evidence.Description,
                CollectionDate = ce.Evidence.CollectionDate,
                Region = ce.Evidence.Region,
                Annotation = ce.Evidence.Annotation,
                Purpose = ce.Evidence.Purpose
            })
            .ToListAsync();

    public async Task<List<EvidenceCaseDto>> GetPendingEvidencesAsync() =>
        await context.CaseEvidences
            .Where(ce => ce.ApprovalStatus == ApprovalStatus.Pending)
            .Select(ce => new EvidenceCaseDto
            {
                EvidenceId = ce.EvidenceId,
                CaseId = ce.CaseId,
                Type = ce.Evidence.Type,
                Description = ce.Evidence.Description,
                CollectionDate = ce.Evidence.CollectionDate,
                Region = ce.Evidence.Region,
                Annotation = ce.Evidence.Annotation,
                Purpose = ce.Evidence.Purpose
            })
            .ToListAsync();

    public async Task LinkEvidenceToCaseAsync(int evidenceId, int caseId)
    {
        var entity = new CaseEvidence
        {
            CaseId = caseId,
            EvidenceId = evidenceId,
            ApprovalStatus = ApprovalStatus.Draft
        };
        context.CaseEvidences.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UnlinkEvidenceFromCaseAsync(int evidenceId, int caseId)
    {
        var entity = await context.CaseEvidences
            .FirstOrDefaultAsync(ce => ce.EvidenceId == evidenceId && ce.CaseId == caseId)
            ?? throw new InvalidOperationException("Evidence not linked to case");

        context.CaseEvidences.Remove(entity);
        await context.SaveChangesAsync();
    }
    #endregion

    #region Suspect
    public async Task<SuspectDto> CreateSuspectAsync(int caseId, CreateSuspectDto dto)
    {
        var suspect = mapper.Map<Suspect>(dto);
        context.Suspects.Add(suspect);

        var link = new CaseSuspect
        {
            CaseId = caseId,
            Suspect = suspect,
            ApprovalStatus = ApprovalStatus.Draft
        };

        context.CaseSuspects.Add(link);
        await context.SaveChangesAsync();

        var result = mapper.Map<SuspectDto>(suspect);
        result.CaseId = caseId;
        return result;
    }

    public async Task<SuspectDto> UpdateSuspectAsync(int suspectId, UpdateSuspectDto dto)
    {
        var suspect = await context.Suspects.FindAsync(suspectId)
            ?? throw new EntityNotFoundException("Suspect", suspectId);

        mapper.Map(dto, suspect);
        await context.SaveChangesAsync();

        return mapper.Map<SuspectDto>(suspect);
    }

    public async Task DeleteSuspectAsync(int suspectId)
    {
        var suspect = await context.Suspects.FindAsync(suspectId)
            ?? throw new EntityNotFoundException("Suspect", suspectId);

        context.Suspects.Remove(suspect);
        await context.SaveChangesAsync();
    }

    public async Task<SuspectDto?> GetSuspectAsync(int suspectId) =>
        await context.Suspects
            .Where(s => s.Id == suspectId)
            .ProjectTo<SuspectDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

    public async Task<List<SuspectDto>> GetSuspectsAsync() =>
        await context.Suspects
            .ProjectTo<SuspectDto>(mapper.ConfigurationProvider)
            .ToListAsync();

    public async Task<List<SuspectDto>> GetSuspectsFromCase(int caseId) =>
        await context.CaseSuspects
            .Where(cs => cs.CaseId == caseId)
            .Select(cs => mapper.Map<SuspectDto>(cs.Suspect))
            .ToListAsync();

    public async Task<List<SuspectDto>> GetApprovedSuspectsAsync() =>
        await context.CaseSuspects
            .Where(cs => cs.ApprovalStatus == ApprovalStatus.Approved)
            .Select(cs => mapper.Map<SuspectDto>(cs.Suspect))
            .ToListAsync();

    public async Task<List<SuspectDto>> GetDeclinedSuspectsAsync() =>
        await context.CaseSuspects
            .Where(cs => cs.ApprovalStatus == ApprovalStatus.Declined)
            .Select(cs => mapper.Map<SuspectDto>(cs.Suspect))
            .ToListAsync();

    public async Task<List<SuspectDto>> GetPendingSuspectsAsync() =>
        await context.CaseSuspects
            .Where(cs => cs.ApprovalStatus == ApprovalStatus.Pending)
            .Select(cs => mapper.Map<SuspectDto>(cs.Suspect))
            .ToListAsync();

    public async Task LinkSuspectToCaseAsync(int suspectId, int caseId)
    {
        var entity = new CaseSuspect
        {
            CaseId = caseId,
            SuspectId = suspectId,
            ApprovalStatus = ApprovalStatus.Draft
        };
        context.CaseSuspects.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UnlinkSuspectFromCaseAsync(int suspectId, int caseId)
    {
        var entity = await context.CaseSuspects
            .FirstOrDefaultAsync(cs => cs.SuspectId == suspectId && cs.CaseId == caseId)
            ?? throw new InvalidOperationException("Suspect not linked to case");

        context.CaseSuspects.Remove(entity);
        await context.SaveChangesAsync();
    }
    #endregion

    #region Expense
    public async Task<ExpenseDto> CreateExpenseAsync(int caseId, CreateExpenseDto dto)
    {
        var expense = mapper.Map<Expense>(dto);
        expense.CaseId = caseId;
        context.Expenses.Add(expense);
        await context.SaveChangesAsync();
        return mapper.Map<ExpenseDto>(expense);
    }

    public async Task<ExpenseDto> UpdateExpenseAsync(int expenseId, UpdateExpenseDto dto)
    {
        var expense = await context.Expenses.FindAsync(expenseId)
            ?? throw new EntityNotFoundException("Expense", expenseId);

        mapper.Map(dto, expense);
        await context.SaveChangesAsync();
        return mapper.Map<ExpenseDto>(expense);
    }

    public async Task DeleteExpenseAsync(int expenseId)
    {
        var expense = await context.Expenses.FindAsync(expenseId)
            ?? throw new EntityNotFoundException("Expense", expenseId);

        context.Expenses.Remove(expense);
        await context.SaveChangesAsync();
    }

    public async Task<ExpenseDto?> GetExpenseAsync(int expenseId) =>
        await context.Expenses
            .Where(e => e.Id == expenseId)
            .ProjectTo<ExpenseDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

    public async Task<List<ExpenseDto>> GetExpensesAsync() =>
        await context.Expenses
            .ProjectTo<ExpenseDto>(mapper.ConfigurationProvider)
            .ToListAsync();

    public async Task<List<ExpenseDto>> GetExpensesFromCaseAsync(int caseId) =>
        await context.Expenses
            .Where(e => e.CaseId == caseId)
            .ProjectTo<ExpenseDto>(mapper.ConfigurationProvider)
            .ToListAsync();

    public async Task<List<ExpenseDto>> GetApprovedExpensesAsync() =>
        await context.Expenses
            .Where(e => e.ApprovalStatus == ApprovalStatus.Approved)
            .ProjectTo<ExpenseDto>(mapper.ConfigurationProvider)
            .ToListAsync();

    public async Task<List<ExpenseDto>> GetDeclinedExpensesAsync() =>
        await context.Expenses
            .Where(e => e.ApprovalStatus == ApprovalStatus.Declined)
            .ProjectTo<ExpenseDto>(mapper.ConfigurationProvider)
            .ToListAsync();

    public async Task<List<ExpenseDto>> GetPendingExpensesAsync() =>
        await context.Expenses
            .Where(e => e.ApprovalStatus == ApprovalStatus.Pending)
            .ProjectTo<ExpenseDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    #endregion

    #region Report
    public async Task<ReportDto> CreateReportAsync(int caseId, CreateReportDto dto)
    {
        var report = mapper.Map<Report>(dto);
        report.CaseId = caseId;
        context.Reports.Add(report);
        await context.SaveChangesAsync();
        return mapper.Map<ReportDto>(report);
    }

    public async Task<ReportDto> UpdateReportAsync(int reportId, UpdateReportDto dto)
    {
        var report = await context.Reports.FindAsync(reportId)
            ?? throw new EntityNotFoundException("Report", reportId);

        mapper.Map(dto, report);
        await context.SaveChangesAsync();
        return mapper.Map<ReportDto>(report);
    }

    public async Task DeleteReportAsync(int reportId)
    {
        var report = await context.Reports.FindAsync(reportId)
            ?? throw new EntityNotFoundException("Report", reportId);

        context.Reports.Remove(report);
        await context.SaveChangesAsync();
    }

    public async Task<ReportDto?> GetReportAsync(int reportId) =>
        await context.Reports
            .Where(r => r.Id == reportId)
            .ProjectTo<ReportDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

    public async Task<List<ReportDto>> GetReportsAsync() =>
        await context.Reports
            .ProjectTo<ReportDto>(mapper.ConfigurationProvider)
            .ToListAsync();

    public async Task<List<ReportDto>> GetReportsFromCaseAsync(int caseId) =>
        await context.Reports
            .Where(r => r.CaseId == caseId)
            .ProjectTo<ReportDto>(mapper.ConfigurationProvider)
            .ToListAsync();

    public async Task<List<ReportDto>> GetApprovedReportsAsync() =>
        await context.Reports
            .Where(r => r.ApprovalStatus == ApprovalStatus.Approved)
            .ProjectTo<ReportDto>(mapper.ConfigurationProvider)
            .ToListAsync();

    public async Task<List<ReportDto>> GetDeclinedReportsAsync() =>
        await context.Reports
            .Where(r => r.ApprovalStatus == ApprovalStatus.Declined)
            .ProjectTo<ReportDto>(mapper.ConfigurationProvider)
            .ToListAsync();

    public async Task<List<ReportDto>> GetPendingReportsAsync() =>
        await context.Reports
            .Where(r => r.ApprovalStatus == ApprovalStatus.Pending)
            .ProjectTo<ReportDto>(mapper.ConfigurationProvider)
            .ToListAsync();

    public async Task<ReportDto> SubmitReportAsync(int reportId)
    {
        var report = await context.Reports.FindAsync(reportId)
            ?? throw new EntityNotFoundException("Report", reportId);

        report.ApprovalStatus = ApprovalStatus.Pending;
        await context.SaveChangesAsync();
        return mapper.Map<ReportDto>(report);
    }

    public async Task<ExpenseDto> SubmitExpenseAsync(int expenseId)
    {
        var expense = await context.Expenses.FindAsync(expenseId)
            ?? throw new EntityNotFoundException("Expense", expenseId);

        expense.ApprovalStatus = ApprovalStatus.Pending;
        await context.SaveChangesAsync();
        return mapper.Map<ExpenseDto>(expense);
    }
    #endregion
}
