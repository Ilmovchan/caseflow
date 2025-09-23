using AutoMapper;
using AutoMapper.QueryableExtensions;
using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Interfaces.IDetective;
using CaseFlow.DAL.Data;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.BLL.Services;

public class DetectiveService : IDetectiveService
{
    private readonly DetectiveAgencyDbContext _context;
    private readonly IMapper _mapper;

    public DetectiveService(DetectiveAgencyDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    #region Cases
    public async Task<Case?> GetCaseAsync(int caseId) =>
        await _context.Cases.FindAsync(caseId);

    public async Task<List<Case>> GetCasesAsync() =>
        await _context.Cases.ToListAsync();

    public async Task<Case> UpdateCaseAsync(int caseId, UpdateCaseByDetectiveDto dto)
    {
        var caseEntity = await _context.Cases.FindAsync(caseId)
            ?? throw new EntityNotFoundException("Case", caseId);

        _mapper.Map(dto, caseEntity);
        await _context.SaveChangesAsync();
        return caseEntity;
    }
    #endregion

    #region Clients
    public async Task<Client?> GetClientAsync(int clientId) =>
        await _context.Clients.FindAsync(clientId);

    public async Task<List<Client>> GetClientsAsync() =>
        await _context.Clients.ToListAsync();
    #endregion

    #region Evidences
    public async Task<EvidenceCaseDto> CreateEvidenceAsync(int caseId, CreateEvidenceDto dto)
    {
        var evidenceEntity = _mapper.Map<Evidence>(dto);
        _context.Evidences.Add(evidenceEntity);

        var caseEvidenceEntity = new CaseEvidence
        {
            CaseId = caseId,
            Evidence = evidenceEntity,
            ApprovalStatus = ApprovalStatus.Draft,
        };

        _context.CaseEvidences.Add(caseEvidenceEntity);
        await _context.SaveChangesAsync();

        var dtoResult = _mapper.Map<EvidenceCaseDto>(evidenceEntity);
        dtoResult.CaseId = caseId;
        dtoResult.ApprovalStatus = ApprovalStatus.Draft;
        return dtoResult;
    }

    public async Task<EvidenceCaseDto> UpdateEvidenceAsync(int evidenceId, UpdateEvidenceDto dto, bool forceClone = true)
    {
        var evidence = await _context.Evidences.FindAsync(evidenceId)
            ?? throw new EntityNotFoundException("Evidence", evidenceId);

        _mapper.Map(dto, evidence);
        await _context.SaveChangesAsync();

        var finalDto = _mapper.Map<EvidenceCaseDto>(evidence);
        finalDto.CaseId = null;
        finalDto.ApprovalStatus = ApprovalStatus.Draft;
        return finalDto;
    }

    public async Task DeleteEvidenceAsync(int evidenceId)
    {
        var evidence = await _context.Evidences.FindAsync(evidenceId)
            ?? throw new EntityNotFoundException("Evidence", evidenceId);

        _context.Evidences.Remove(evidence);
        await _context.SaveChangesAsync();
    }

    public async Task<List<EvidenceCaseDto>> GetEvidencesAsync() =>
        await _context.CaseEvidences
            .Select(ce => new EvidenceCaseDto
            {
                EvidenceId = ce.EvidenceId,
                CaseId = ce.CaseId,
                Type = ce.Evidence.Type,
                Description = ce.Evidence.Description,
                CollectionDate = ce.Evidence.CollectionDate,
                Region = ce.Evidence.Region,
                Annotation = ce.Evidence.Annotation,
                Purpose = ce.Evidence.Purpose,
                ApprovalStatus = ce.ApprovalStatus,
            })
            .ToListAsync();

    public async Task<EvidenceCaseDto?> GetEvidenceAsync(int evidenceId) =>
        await _context.CaseEvidences
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
                Purpose = ce.Evidence.Purpose,
                ApprovalStatus = ce.ApprovalStatus,
            })
            .FirstOrDefaultAsync();

    public async Task<List<EvidenceCaseDto>> GetEvidencesFromCase(int caseId) =>
        await _context.CaseEvidences
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
                Purpose = ce.Evidence.Purpose,
                ApprovalStatus = ce.ApprovalStatus,
            })
            .ToListAsync();

    public async Task LinkEvidenceToCaseAsync(int evidenceId, int caseId)
    {
        var isAlreadyLinked = await _context.CaseEvidences
            .AnyAsync(ce => ce.EvidenceId == evidenceId && ce.CaseId == caseId);

        if (isAlreadyLinked)
            throw new InvalidOperationException("Already linked");

        _context.CaseEvidences.Add(new CaseEvidence
        {
            CaseId = caseId,
            EvidenceId = evidenceId,
            ApprovalStatus = ApprovalStatus.Draft,
        });
        await _context.SaveChangesAsync();
    }

    public async Task UnlinkEvidenceFromCaseAsync(int evidenceId, int caseId)
    {
        var caseEvidenceEntity = await _context.CaseEvidences
            .FirstOrDefaultAsync(ce => ce.EvidenceId == evidenceId && ce.CaseId == caseId)
            ?? throw new InvalidOperationException("Not linked");

        _context.CaseEvidences.Remove(caseEvidenceEntity);
        await _context.SaveChangesAsync();
    }
    #endregion

    #region Suspects
    public async Task<SuspectDto> CreateSuspectAsync(int caseId, CreateSuspectDto dto)
    {
        var suspectEntity = _mapper.Map<Suspect>(dto);
        _context.Suspects.Add(suspectEntity);

        var caseSuspectEntity = new CaseSuspect
        {
            CaseId = caseId,
            Suspect = suspectEntity,
            ApprovalStatus = ApprovalStatus.Draft,
        };

        _context.CaseSuspects.Add(caseSuspectEntity);
        await _context.SaveChangesAsync();

        var dtoResult = _mapper.Map<SuspectDto>(suspectEntity);
        dtoResult.CaseId = caseId;
        dtoResult.ApprovalStatus = ApprovalStatus.Draft;
        return dtoResult;
    }

    public async Task<SuspectDto> UpdateSuspectAsync(int suspectId, UpdateSuspectDto dto)
    {
        var suspectEntity = await _context.Suspects.FindAsync(suspectId)
            ?? throw new EntityNotFoundException("Suspect", suspectId);

        _mapper.Map(dto, suspectEntity);
        await _context.SaveChangesAsync();

        return _mapper.Map<SuspectDto>(suspectEntity);
    }

    public async Task DeleteSuspectAsync(int suspectId)
    {
        var suspectEntity = await _context.Suspects.FindAsync(suspectId)
            ?? throw new EntityNotFoundException("Suspect", suspectId);

        _context.Suspects.Remove(suspectEntity);
        await _context.SaveChangesAsync();
    }

    public async Task<List<SuspectDto>> GetSuspectsAsync() =>
        await _context.Suspects.ProjectTo<SuspectDto>(_mapper.ConfigurationProvider).ToListAsync();

    public async Task<SuspectDto?> GetSuspectAsync(int suspectId) =>
        await _context.Suspects
            .Where(s => s.Id == suspectId)
            .ProjectTo<SuspectDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

    public async Task<List<SuspectDto>> GetSuspectsFromCaseAsync(int caseId) =>
        await _context.CaseSuspects
            .Where(cs => cs.CaseId == caseId)
            .ProjectTo<SuspectDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

    public async Task LinkSuspectToCaseAsync(int suspectId, int caseId)
    {
        var isAlreadyLinked = await _context.CaseSuspects
            .AnyAsync(cs => cs.SuspectId == suspectId && cs.CaseId == caseId);

        if (isAlreadyLinked)
            throw new InvalidOperationException("Already linked");

        _context.CaseSuspects.Add(new CaseSuspect
        {
            CaseId = caseId,
            SuspectId = suspectId,
            ApprovalStatus = ApprovalStatus.Draft
        });
        await _context.SaveChangesAsync();
    }

    public async Task UnlinkSuspectFromCaseAsync(int suspectId, int caseId)
    {
        var caseSuspectEntity = await _context.CaseSuspects
            .FirstOrDefaultAsync(cs => cs.SuspectId == suspectId && cs.CaseId == caseId)
            ?? throw new InvalidOperationException("Not linked");

        _context.CaseSuspects.Remove(caseSuspectEntity);
        await _context.SaveChangesAsync();
    }
    #endregion

    #region Expenses
    public async Task<ExpenseDto> CreateExpenseAsync(int caseId, CreateExpenseDto dto)
    {
        var expenseEntity = _mapper.Map<Expense>(dto);
        expenseEntity.CaseId = caseId;

        _context.Expenses.Add(expenseEntity);
        await _context.SaveChangesAsync();

        return _mapper.Map<ExpenseDto>(expenseEntity);
    }

    public async Task<ExpenseDto> UpdateExpenseAsync(int expenseId, UpdateExpenseDto dto)
    {
        var expenseEntity = await _context.Expenses.FindAsync(expenseId)
            ?? throw new EntityNotFoundException("Expense", expenseId);

        _mapper.Map(dto, expenseEntity);
        await _context.SaveChangesAsync();

        return _mapper.Map<ExpenseDto>(expenseEntity);
    }

    public async Task DeleteExpenseAsync(int expenseId)
    {
        var expenseEntity = await _context.Expenses.FindAsync(expenseId)
            ?? throw new EntityNotFoundException("Expense", expenseId);

        _context.Expenses.Remove(expenseEntity);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ExpenseDto>> GetExpensesAsync() =>
        await _context.Expenses.ProjectTo<ExpenseDto>(_mapper.ConfigurationProvider).ToListAsync();

    public async Task<ExpenseDto?> GetExpenseAsync(int expenseId) =>
        await _context.Expenses
            .Where(e => e.Id == expenseId)
            .ProjectTo<ExpenseDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
    #endregion

    #region Reports
    public async Task<ReportDto> CreateReportAsync(int caseId, CreateReportDto dto)
    {
        var reportEntity = _mapper.Map<Report>(dto);
        reportEntity.CaseId = caseId;

        _context.Reports.Add(reportEntity);
        await _context.SaveChangesAsync();

        return _mapper.Map<ReportDto>(reportEntity);
    }

    public async Task<ReportDto> UpdateReportAsync(int reportId, UpdateReportDto dto)
    {
        var reportEntity = await _context.Reports.FindAsync(reportId)
            ?? throw new EntityNotFoundException("Report", reportId);

        _mapper.Map(dto, reportEntity);
        await _context.SaveChangesAsync();

        return _mapper.Map<ReportDto>(reportEntity);
    }

    public async Task DeleteReportAsync(int reportId)
    {
        var reportEntity = await _context.Reports.FindAsync(reportId)
            ?? throw new EntityNotFoundException("Report", reportId);

        _context.Reports.Remove(reportEntity);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ReportDto>> GetReportsAsync() =>
        await _context.Reports.ProjectTo<ReportDto>(_mapper.ConfigurationProvider).ToListAsync();

    public async Task<ReportDto?> GetReportAsync(int reportId) =>
        await _context.Reports
            .Where(r => r.Id == reportId)
            .ProjectTo<ReportDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
    #endregion
}
