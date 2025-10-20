using AutoMapper;
using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.CaseType;
using CaseFlow.BLL.Dto.Client;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Dto.Detective;
using CaseFlow.BLL.Exceptions;
using CaseFlow.DAL.Data;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.BLL.Services;

public class AdminService(DetectiveAgencyDbContext context, IMapper mapper)
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

    public async Task<PagedResult<Case>> GetCasesPagedAsync(int pageNumber = 1, int pageSize = 15)
    {
        var totalCount = await context.Cases.CountAsync();
        var items = await context.Cases
            .Include(c => c.CaseType)
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .OrderBy(c => c.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Case>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<Case> CreateCaseAsync(CreateCaseDto dto)
    {
        if (!await context.Clients.AnyAsync(e => e.Id == dto.ClientId))
            throw new ArgumentException($"Client with id {dto.ClientId} does not exist");

        if (!await context.CaseTypes.AnyAsync(ct => ct.Id == dto.CaseTypeId))
            throw new ArgumentException($"CaseType with id {dto.CaseTypeId} does not exist");

        if (dto.DetectiveId.HasValue &&
            !await context.Detectives.AnyAsync(d => d.Id == dto.DetectiveId.Value))
            throw new ArgumentException($"Detective with id {dto.DetectiveId} does not exist");

        var caseEntity = mapper.Map<Case>(dto);
        context.Cases.Add(caseEntity);
        await context.SaveChangesAsync();

        return caseEntity;
    }

    public async Task<Case> UpdateCaseAsync(int id, UpdateCaseByAdminDto dto)
    {
        var caseEntity = await context.Cases.FindAsync(id)
                         ?? throw new EntityNotFoundException("Case", id);

        mapper.Map(dto, caseEntity);
        await context.SaveChangesAsync();

        return caseEntity;
    }

    public async Task DeleteCaseAsync(int caseId)
    {
        var caseEntity = await context.Cases.FindAsync(caseId)
                         ?? throw new EntityNotFoundException("Case", caseId);

        context.CaseEvidences.RemoveRange(await context.CaseEvidences.Where(ce => ce.CaseId == caseId).ToListAsync());
        context.CaseSuspects.RemoveRange(await context.CaseSuspects.Where(cs => cs.CaseId == caseId).ToListAsync());
        context.Reports.RemoveRange(await context.Reports.Where(r => r.CaseId == caseId).ToListAsync());
        context.Expenses.RemoveRange(await context.Expenses.Where(e => e.CaseId == caseId).ToListAsync());

        context.Cases.Remove(caseEntity);
        await context.SaveChangesAsync();
    }

    #endregion

    #region Client

    public async Task<Client?> GetClientAsync(int clientId) =>
        await context.Clients.FindAsync(clientId);

    public async Task<List<Client>> GetClientsAsync() =>
        await context.Clients.ToListAsync();

    public async Task<PagedResult<Client>> GetClientsPagedAsync(int pageNumber = 1, int pageSize = 15)
    {
        var totalCount = await context.Clients.CountAsync();
        var items = await context.Clients
            .OrderBy(c => c.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Client>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<Client> CreateClientAsync(CreateClientDto dto)
    {
        var clientEntity = mapper.Map<Client>(dto);
        clientEntity.RegistrationDate = DateTime.UtcNow;

        context.Clients.Add(clientEntity);
        await context.SaveChangesAsync();

        return clientEntity;
    }

    public async Task<Client> UpdateClientAsync(int id, UpdateClientDto dto)
    {
        var clientEntity = await context.Clients.FindAsync(id)
                           ?? throw new EntityNotFoundException("Client", id);

        mapper.Map(dto, clientEntity);
        await context.SaveChangesAsync();

        return clientEntity;
    }

    public async Task DeleteClientAsync(int clientId)
    {
        var clientEntity = await context.Clients.FindAsync(clientId)
                           ?? throw new EntityNotFoundException("Client", clientId);

        var connectedCases = await context.Cases.Where(c => c.ClientId == clientId).ToListAsync();
        if (connectedCases.Count > 0)
            throw new EntityDeleteConflictException("Client", clientId, connectedCases.Select(c => c.Id));

        context.Clients.Remove(clientEntity);
        await context.SaveChangesAsync();
    }

    #endregion

    #region Detective

    public async Task<Detective?> GetDetectiveAsync(int detectiveId) =>
        await context.Detectives.FindAsync(detectiveId);

    public async Task<List<Detective>> GetDetectivesAsync() =>
        await context.Detectives.ToListAsync();

    public async Task<PagedResult<Detective>> GetDetectivesPagedAsync(int pageNumber = 1, int pageSize = 15)
    {
        var totalCount = await context.Detectives.CountAsync();
        var items = await context.Detectives
            .OrderBy(d => d.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Detective>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<Detective>> GetUnassignedDetectivesAsync() =>
        await context.Detectives
            .Where(d => !context.Cases.Any(c => c.DetectiveId == d.Id))
            .ToListAsync();

    public async Task<Detective> CreateDetectiveAsync(CreateDetectiveDto dto)
    {
        var detectiveEntity = mapper.Map<Detective>(dto);
        detectiveEntity.Status = DetectiveStatus.Active;
        detectiveEntity.HireDate = DateTime.UtcNow;

        context.Detectives.Add(detectiveEntity);
        await context.SaveChangesAsync();

        // PostgreSQL: CREATE ROLE detective_ivanov LOGIN PASSWORD '...' INHERIT;
        // GRANT detective TO detective_ivanov;

        return detectiveEntity;
    }

    public async Task<Detective> UpdateDetectiveAsync(int id, UpdateDetectiveDto dto)
    {
        var detectiveEntity = await context.Detectives.FindAsync(id)
                              ?? throw new EntityNotFoundException("Detective", id);

        mapper.Map(dto, detectiveEntity);
        await context.SaveChangesAsync();

        return detectiveEntity;
    }

    public async Task DeleteDetectiveAsync(int detectiveId)
    {
        var detectiveEntity = await context.Detectives.FindAsync(detectiveId)
                              ?? throw new EntityNotFoundException("Detective", detectiveId);

        var cases = await context.Cases.Where(c => c.DetectiveId == detectiveId).ToListAsync();
        foreach (var c in cases)
            c.DetectiveId = null;

        context.Detectives.Remove(detectiveEntity);
        await context.SaveChangesAsync();

        // PostgreSQL: DROP ROLE detective_ivanov;
    }

    public async Task<(Case, Detective)> AssignDetectiveAsync(int caseId, int detectiveId)
    {
        var caseEntity = await context.Cases.FindAsync(caseId)
                         ?? throw new EntityNotFoundException("Case", caseId);

        var detectiveEntity = await context.Detectives.FindAsync(detectiveId)
                                ?? throw new EntityNotFoundException("Detective", detectiveId);

        caseEntity.DetectiveId = detectiveId;
        await context.SaveChangesAsync();

        return (caseEntity, detectiveEntity);
    }

    public async Task DismissDetectiveAsync(int caseId)
    {
        var caseEntity = await context.Cases.FindAsync(caseId)
                         ?? throw new EntityNotFoundException("Case", caseId);

        caseEntity.DetectiveId = null;
        await context.SaveChangesAsync();
    }

    #endregion

    #region Evidence

    public async Task<Evidence?> GetEvidenceAsync(int evidenceId) =>
        await context.Evidences.FindAsync(evidenceId);

    public async Task<List<Evidence>> GetEvidencesAsync() =>
        await context.Evidences.ToListAsync();

    public async Task<PagedResult<Evidence>> GetEvidencesPagedAsync(int pageNumber = 1, int pageSize = 15)
    {
        var totalCount = await context.Evidences.CountAsync();
        var items = await context.Evidences
            .OrderBy(e => e.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Evidence>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<Evidence>> GetEvidencesFromCaseAsync(int caseId) =>
        await context.CaseEvidences.Where(ce => ce.CaseId == caseId).Select(ce => ce.Evidence).ToListAsync();

    public async Task<List<Evidence>> GetPendingEvidencesAsync() =>
        await context.CaseEvidences
            .Where(ce => ce.ApprovalStatus == ApprovalStatus.Pending)
            .Select(ce => ce.Evidence)
            .ToListAsync();

    public async Task<Evidence> ApproveEvidenceAsync(int evidenceId)
    {
        // First try to find through junction table
        var caseEvidence = await context.CaseEvidences
            .FirstOrDefaultAsync(ce => ce.EvidenceId == evidenceId && ce.ApprovalStatus == ApprovalStatus.Pending);
        
        if (caseEvidence != null)
        {
            caseEvidence.ApprovalStatus = ApprovalStatus.Approved;
            await context.SaveChangesAsync();
            return caseEvidence.Evidence;
        }
        
        // If not found in junction table, this might be a sample data case
        // For now, just return the evidence (in a real scenario, you'd handle this differently)
        var evidence = await context.Evidences.FindAsync(evidenceId)
                     ?? throw new EntityNotFoundException("Evidence", evidenceId);
        
        return evidence;
    }

    public async Task<Evidence> RejectEvidenceAsync(int evidenceId)
    {
        // First try to find through junction table
        var caseEvidence = await context.CaseEvidences
            .FirstOrDefaultAsync(ce => ce.EvidenceId == evidenceId && ce.ApprovalStatus == ApprovalStatus.Pending);
        
        if (caseEvidence != null)
        {
            caseEvidence.ApprovalStatus = ApprovalStatus.Declined;
            await context.SaveChangesAsync();
            return caseEvidence.Evidence;
        }
        
        // If not found in junction table, this might be a sample data case
        // For now, just return the evidence (in a real scenario, you'd handle this differently)
        var evidence = await context.Evidences.FindAsync(evidenceId)
                     ?? throw new EntityNotFoundException("Evidence", evidenceId);
        
        return evidence;
    }

    // Evidence approval methods removed - Evidence entity no longer has ApprovalStatus
    // Approval status is managed through CaseEvidence junction table

    #endregion

    #region Suspect

    public async Task<Suspect?> GetSuspectAsync(int suspectId) =>
        await context.Suspects.FindAsync(suspectId);

    public async Task<List<Suspect>> GetSuspectsAsync() =>
        await context.Suspects.ToListAsync();

    public async Task<PagedResult<Suspect>> GetSuspectsPagedAsync(int pageNumber = 1, int pageSize = 15)
    {
        var totalCount = await context.Suspects.CountAsync();
        var items = await context.Suspects
            .OrderBy(s => s.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Suspect>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<Suspect>> GetSuspectsFromCaseAsync(int caseId) =>
        await context.CaseSuspects.Where(cs => cs.CaseId == caseId).Select(cs => cs.Suspect).ToListAsync();

    public async Task<List<Suspect>> GetPendingSuspectsAsync() =>
        await context.CaseSuspects
            .Where(cs => cs.ApprovalStatus == ApprovalStatus.Pending)
            .Select(cs => cs.Suspect)
            .ToListAsync();

    public async Task<Suspect> ApproveSuspectAsync(int suspectId)
    {
        // First try to find through junction table
        var caseSuspect = await context.CaseSuspects
            .FirstOrDefaultAsync(cs => cs.SuspectId == suspectId && cs.ApprovalStatus == ApprovalStatus.Pending);
        
        if (caseSuspect != null)
        {
            caseSuspect.ApprovalStatus = ApprovalStatus.Approved;
            await context.SaveChangesAsync();
            return caseSuspect.Suspect;
        }
        
        // If not found in junction table, this might be a sample data case
        // For now, just return the suspect (in a real scenario, you'd handle this differently)
        var suspect = await context.Suspects.FindAsync(suspectId)
                     ?? throw new EntityNotFoundException("Suspect", suspectId);
        
        return suspect;
    }

    public async Task<Suspect> RejectSuspectAsync(int suspectId)
    {
        // First try to find through junction table
        var caseSuspect = await context.CaseSuspects
            .FirstOrDefaultAsync(cs => cs.SuspectId == suspectId && cs.ApprovalStatus == ApprovalStatus.Pending);
        
        if (caseSuspect != null)
        {
            caseSuspect.ApprovalStatus = ApprovalStatus.Declined;
            await context.SaveChangesAsync();
            return caseSuspect.Suspect;
        }
        
        // If not found in junction table, this might be a sample data case
        // For now, just return the suspect (in a real scenario, you'd handle this differently)
        var suspect = await context.Suspects.FindAsync(suspectId)
                     ?? throw new EntityNotFoundException("Suspect", suspectId);
        
        return suspect;
    }

    // Suspect approval methods removed - Suspect entity no longer has ApprovalStatus
    // Approval status is managed through CaseSuspect junction table

    #endregion

    #region Expense

    public async Task<Expense?> GetExpenseAsync(int expenseId) =>
        await context.Expenses.FindAsync(expenseId);

    public async Task<List<Expense>> GetExpensesAsync() =>
        await context.Expenses.ToListAsync();

    public async Task<PagedResult<Expense>> GetExpensesPagedAsync(int pageNumber = 1, int pageSize = 15)
    {
        var totalCount = await context.Expenses.CountAsync();
        var items = await context.Expenses
            .OrderBy(e => e.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Expense>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<Expense>> GetExpensesFromCaseAsync(int caseId) =>
        await context.Expenses.Where(e => e.CaseId == caseId).ToListAsync();

    public async Task<List<Expense>> GetPendingExpensesAsync() =>
        await context.Expenses.Where(e => e.ApprovalStatus == ApprovalStatus.Pending).ToListAsync();

    public async Task<Expense> ApproveExpenseAsync(int expenseId)
    {
        var expense = await context.Expenses.FindAsync(expenseId)
                     ?? throw new EntityNotFoundException("Expense", expenseId);
        
        expense.ApprovalStatus = ApprovalStatus.Approved;
        await context.SaveChangesAsync();
        
        return expense;
    }

    public async Task<Expense> RejectExpenseAsync(int expenseId)
    {
        var expense = await context.Expenses.FindAsync(expenseId)
                     ?? throw new EntityNotFoundException("Expense", expenseId);
        
        expense.ApprovalStatus = ApprovalStatus.Declined;
        await context.SaveChangesAsync();
        
        return expense;
    }

    #endregion

    #region Report

    public async Task<Report?> GetReportAsync(int reportId) =>
        await context.Reports.FindAsync(reportId);

    public async Task<List<Report>> GetReportsAsync() =>
        await context.Reports.ToListAsync();

    public async Task<PagedResult<Report>> GetReportsPagedAsync(int pageNumber = 1, int pageSize = 15)
    {
        var totalCount = await context.Reports.CountAsync();
        var items = await context.Reports
            .OrderBy(r => r.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Report>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<Report>> GetReportsFromCaseAsync(int caseId) =>
        await context.Reports.Where(r => r.CaseId == caseId).ToListAsync();

    public async Task<List<Report>> GetPendingReportsAsync() =>
        await context.Reports.Where(r => r.ApprovalStatus == ApprovalStatus.Pending).ToListAsync();

    public async Task<Report> ApproveReportAsync(int reportId)
    {
        var report = await context.Reports.FindAsync(reportId)
                    ?? throw new EntityNotFoundException("Report", reportId);
        
        report.ApprovalStatus = ApprovalStatus.Approved;
        await context.SaveChangesAsync();
        
        return report;
    }

    public async Task<Report> RejectReportAsync(int reportId)
    {
        var report = await context.Reports.FindAsync(reportId)
                    ?? throw new EntityNotFoundException("Report", reportId);
        
        report.ApprovalStatus = ApprovalStatus.Declined;
        await context.SaveChangesAsync();
        
        return report;
    }

    #endregion

    #region CaseType

    public async Task<CaseType?> GetCaseTypeAsync(int caseTypeId) =>
        await context.CaseTypes.FindAsync(caseTypeId);

    public async Task<List<CaseType>> GetCaseTypesAsync() =>
        await context.CaseTypes.ToListAsync();

    public async Task<PagedResult<CaseType>> GetCaseTypesPagedAsync(int pageNumber = 1, int pageSize = 15)
    {
        var totalCount = await context.CaseTypes.CountAsync();
        var items = await context.CaseTypes
            .OrderBy(ct => ct.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<CaseType>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<CaseType> CreateCaseTypeAsync(CreateCaseTypeDto dto)
    {
        var caseTypeEntity = mapper.Map<CaseType>(dto);
        context.Add(caseTypeEntity);
        await context.SaveChangesAsync();
        return caseTypeEntity;
    }

    public async Task<CaseType> UpdateCaseTypeAsync(int id, UpdateCaseTypeDto dto)
    {
        var caseTypeEntity = await context.CaseTypes.FindAsync(id)
                              ?? throw new EntityNotFoundException("CaseType", id);

        mapper.Map(dto, caseTypeEntity);
        await context.SaveChangesAsync();
        return caseTypeEntity;
    }

    public async Task DeleteCaseTypeAsync(int caseTypeId)
    {
        var caseTypeEntity = await context.CaseTypes.FindAsync(caseTypeId)
                              ?? throw new EntityNotFoundException("CaseType", caseTypeId);

        var connectedCases = await context.Cases.Where(c => c.CaseTypeId == caseTypeId).ToListAsync();
        if (connectedCases.Count > 0)
            throw new EntityDeleteConflictException("CaseType", caseTypeId, connectedCases.Select(c => c.Id));

        context.CaseTypes.Remove(caseTypeEntity);
        await context.SaveChangesAsync();
    }

    #endregion
}
