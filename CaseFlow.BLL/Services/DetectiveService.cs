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
    /// <summary>Login uses PostgreSQL role name (<c>detective.postgres_login</c>) stored in claims as Email; also match <c>detective.email</c>.</summary>
    private static string NormalizeIdentity(string identity) => identity.Trim().ToLowerInvariant();

    private async Task<Detective?> FindDetectiveByIdentityAsync(string identity)
    {
        var key = NormalizeIdentity(identity);
        return await context.Detectives.FirstOrDefaultAsync(d =>
            (d.PostgresLogin != null && d.PostgresLogin.ToLower() == key)
            || d.Email.ToLower() == key);
    }

    private async Task<HashSet<int>> GetAllowedCaseIdsForDetectiveIdentityAsync(string identity)
    {
        var det = await FindDetectiveByIdentityAsync(identity);
        if (det == null) return [];
        var ids = await context.Cases.Where(c => c.DetectiveId == det.Id).Select(c => c.Id).ToListAsync();
        return ids.ToHashSet();
    }

    private async Task EnsureCaseAllowedForDetectiveAsync(string identity, int caseId)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(identity);
        if (!allowed.Contains(caseId))
            throw new EntityNotFoundException("Case", caseId);
    }

    private async Task EnsureExpenseAllowedForDetectiveAsync(string identity, int expenseId)
    {
        var expense = await context.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == expenseId)
                      ?? throw new EntityNotFoundException("Expense", expenseId);
        await EnsureCaseAllowedForDetectiveAsync(identity, expense.CaseId);
    }

    private async Task EnsureReportAllowedForDetectiveAsync(string identity, int reportId)
    {
        var report = await context.Reports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reportId)
                     ?? throw new EntityNotFoundException("Report", reportId);
        await EnsureCaseAllowedForDetectiveAsync(identity, report.CaseId);
    }

    /// <summary>Update/delete/submit when evidence is linked only to this detective's cases.</summary>
    private async Task EnsureEvidenceOnlyLinkedToAllowedCasesAsync(string identity, int evidenceId)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(identity);
        var caseIds = await context.CaseEvidences.Where(ce => ce.EvidenceId == evidenceId).Select(ce => ce.CaseId).ToListAsync();
        if (caseIds.Count == 0)
            throw new EntityNotFoundException("CaseEvidence", evidenceId);
        if (caseIds.Any(cid => !allowed.Contains(cid)))
            throw new EntityNotFoundException("Evidence", evidenceId);
    }

    /// <summary>Edit evidence if it has no links or at least one link to an allowed case.</summary>
    private async Task EnsureEvidenceEditableByDetectiveAsync(string identity, int evidenceId)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(identity);
        var caseIds = await context.CaseEvidences.Where(ce => ce.EvidenceId == evidenceId).Select(ce => ce.CaseId).ToListAsync();
        if (caseIds.Count == 0)
            return;
        if (caseIds.All(cid => !allowed.Contains(cid)))
            throw new EntityNotFoundException("Evidence", evidenceId);
    }

    private async Task EnsureSuspectOnlyLinkedToAllowedCasesAsync(string identity, int suspectId)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(identity);
        var caseIds = await context.CaseSuspects.Where(cs => cs.SuspectId == suspectId).Select(cs => cs.CaseId).ToListAsync();
        if (caseIds.Count == 0)
            throw new EntityNotFoundException("CaseSuspect", suspectId);
        if (caseIds.Any(cid => !allowed.Contains(cid)))
            throw new EntityNotFoundException("Suspect", suspectId);
    }

    private async Task EnsureSuspectEditableByDetectiveAsync(string identity, int suspectId)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(identity);
        var caseIds = await context.CaseSuspects.Where(cs => cs.SuspectId == suspectId).Select(cs => cs.CaseId).ToListAsync();
        if (caseIds.Count == 0)
            return;
        if (caseIds.All(cid => !allowed.Contains(cid)))
            throw new EntityNotFoundException("Suspect", suspectId);
    }

    #region Case
    public async Task<Case?> GetCaseAsync(int caseId) =>
        await context.Cases
            .Include(c => c.CaseType)
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .FirstOrDefaultAsync(c => c.Id == caseId);

    /// <summary>Case details for detective UI: only if the case is assigned to this detective.</summary>
    public async Task<Case?> GetCaseForDetectiveAsync(int caseId, string detectiveIdentity)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        if (!allowed.Contains(caseId))
            return null;
        return await GetCaseAsync(caseId);
    }

    public async Task<List<Case>> GetCasesAsync() =>
        await context.Cases
            .Include(c => c.CaseType)
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .ToListAsync();

    public async Task<List<Case>> GetCasesByDetectiveEmailAsync(string detectiveIdentity)
    {
        var key = NormalizeIdentity(detectiveIdentity);
        return await context.Cases
            .Include(c => c.CaseType)
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .Where(c => c.Detective != null
                        && c.DetectiveId != null
                        && ((c.Detective.PostgresLogin != null && c.Detective.PostgresLogin.ToLower() == key)
                            || c.Detective.Email.ToLower() == key))
            .ToListAsync();
    }

    public async Task<Case> UpdateCaseAsync(int caseId, UpdateCaseByDetectiveDto dto, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
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

    /// <summary>Clients that have at least one case assigned to this detective.</summary>
    public async Task<List<Client>> GetClientsForDetectiveAsync(string detectiveIdentity)
    {
        var caseIds = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        if (caseIds.Count == 0)
            return [];
        var clientIds = await context.Cases.Where(c => caseIds.Contains(c.Id)).Select(c => c.ClientId).Distinct().ToListAsync();
        return await context.Clients.Where(cl => clientIds.Contains(cl.Id)).OrderBy(cl => cl.LastName).ThenBy(cl => cl.FirstName).ToListAsync();
    }

    public async Task<Client?> GetClientForDetectiveAsync(int clientId, string detectiveIdentity)
    {
        var caseIds = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        if (caseIds.Count == 0)
            return null;
        var ok = await context.Cases.AnyAsync(c => caseIds.Contains(c.Id) && c.ClientId == clientId);
        if (!ok)
            return null;
        return await context.Clients.FindAsync(clientId);
    }
    #endregion

    #region DetectiveAccount
    public async Task<Detective?> GetDetectiveByEmailAsync(string identity) =>
        await FindDetectiveByIdentityAsync(identity);
    #endregion

    #region Evidence
    public async Task<EvidenceCaseDto> CreateEvidenceAsync(int caseId, CreateEvidenceDto dto, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
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
        result.EvidenceId = evidenceEntity.Id;
        result.ApprovalStatus = ApprovalStatus.Draft;
        return result;
    }

    public async Task<EvidenceCaseDto> UpdateEvidenceAsync(int evidenceId, UpdateEvidenceDto dto, string detectiveIdentity)
    {
        await EnsureEvidenceEditableByDetectiveAsync(detectiveIdentity, evidenceId);
        var evidence = await context.Evidences.FindAsync(evidenceId)
            ?? throw new EntityNotFoundException("Evidence", evidenceId);
        
        var caseEvidences = await context.CaseEvidences
            .Where(ce => ce.EvidenceId == evidenceId)
            .ToListAsync();
        var caseEvidence = caseEvidences
            .OrderBy(ce => ce.CaseId)
            .FirstOrDefault()
            ?? throw new EntityNotFoundException("CaseEvidence", evidenceId);

        mapper.Map(dto, evidence);
        // Keep workflow consistent even if entity is linked to multiple cases.
        foreach (var ce in caseEvidences)
        {
            ce.ApprovalStatus = ApprovalStatus.Draft;
        }
        await context.SaveChangesAsync();

        var result = mapper.Map<EvidenceCaseDto>(evidence);
        result.EvidenceId = evidence.Id;
        result.CaseId = caseEvidence.CaseId;
        result.ApprovalStatus = caseEvidence.ApprovalStatus;
        return result;
    }

    public async Task DeleteEvidenceAsync(int evidenceId, string detectiveIdentity)
    {
        await EnsureEvidenceOnlyLinkedToAllowedCasesAsync(detectiveIdentity, evidenceId);
        var evidence = await context.Evidences.FindAsync(evidenceId)
            ?? throw new EntityNotFoundException("Evidence", evidenceId);

        var links = await context.CaseEvidences
            .Where(ce => ce.EvidenceId == evidenceId)
            .ToListAsync();
        context.CaseEvidences.RemoveRange(links);
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
                Purpose = ce.Evidence.Purpose,
                ApprovalStatus = ce.ApprovalStatus
            })
            .FirstOrDefaultAsync();

    /// <summary>All evidence rows for detective panel (global catalog); <see cref="EvidenceCaseDto.CaseId"/> is a representative link if any.</summary>
    public async Task<List<EvidenceCaseDto>> GetEvidencesAsync()
    {
        var evidences = await context.Evidences.AsNoTracking().OrderBy(e => e.Id).ToListAsync();
        var allLinks = await context.CaseEvidences.AsNoTracking().ToListAsync();
        var firstByEvidence = allLinks
            .GroupBy(ce => ce.EvidenceId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.CaseId).First());

        return evidences.Select(e =>
        {
            firstByEvidence.TryGetValue(e.Id, out var ce);
            return new EvidenceCaseDto
            {
                EvidenceId = e.Id,
                CaseId = ce?.CaseId,
                Type = e.Type,
                Description = e.Description,
                CollectionDate = e.CollectionDate,
                Region = e.Region,
                Annotation = e.Annotation,
                Purpose = e.Purpose,
                ApprovalStatus = ce?.ApprovalStatus ?? ApprovalStatus.Draft
            };
        }).ToList();
    }

    public async Task<List<EvidenceCaseDto>> GetEvidencesFromCase(int caseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        return await GetEvidencesFromCaseInternalAsync(caseId);
    }

    private async Task<List<EvidenceCaseDto>> GetEvidencesFromCaseInternalAsync(int caseId) =>
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
                Purpose = ce.Evidence.Purpose,
                ApprovalStatus = ce.ApprovalStatus
            })
            .ToListAsync();

    public async Task<List<EvidenceCaseDto>> GetApprovedEvidencesAsync(string detectiveIdentity)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return await context.CaseEvidences
            .Where(ce => allowed.Contains(ce.CaseId) && ce.ApprovalStatus == ApprovalStatus.Approved)
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
                ApprovalStatus = ce.ApprovalStatus
            })
            .ToListAsync();
    }

    public async Task<List<EvidenceCaseDto>> GetDeclinedEvidencesAsync(string detectiveIdentity)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return await context.CaseEvidences
            .Where(ce => allowed.Contains(ce.CaseId) && ce.ApprovalStatus == ApprovalStatus.Declined)
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
                ApprovalStatus = ce.ApprovalStatus
            })
            .ToListAsync();
    }

    public async Task<List<EvidenceCaseDto>> GetPendingEvidencesAsync(string detectiveIdentity)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return await context.CaseEvidences
            .Where(ce => allowed.Contains(ce.CaseId) && ce.ApprovalStatus == ApprovalStatus.Pending)
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
                ApprovalStatus = ce.ApprovalStatus
            })
            .ToListAsync();
    }

    public async Task LinkEvidenceToCaseAsync(int evidenceId, int caseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        var entity = new CaseEvidence
        {
            CaseId = caseId,
            EvidenceId = evidenceId,
            ApprovalStatus = ApprovalStatus.Draft
        };
        context.CaseEvidences.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UnlinkEvidenceFromCaseAsync(int evidenceId, int caseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        var entity = await context.CaseEvidences
            .FirstOrDefaultAsync(ce => ce.EvidenceId == evidenceId && ce.CaseId == caseId)
            ?? throw new InvalidOperationException("Evidence not linked to case");

        context.CaseEvidences.Remove(entity);
        await context.SaveChangesAsync();
    }

    public async Task<EvidenceCaseDto> SubmitEvidenceAsync(int evidenceId, string detectiveIdentity)
    {
        await EnsureEvidenceOnlyLinkedToAllowedCasesAsync(detectiveIdentity, evidenceId);
        var caseEvidences = await context.CaseEvidences
            .Where(ce => ce.EvidenceId == evidenceId)
            .ToListAsync();
        var caseEvidence = caseEvidences
            .OrderBy(ce => ce.CaseId)
            .FirstOrDefault()
            ?? throw new EntityNotFoundException("CaseEvidence", evidenceId);

        foreach (var ce in caseEvidences)
        {
            ce.ApprovalStatus = ApprovalStatus.Pending;
        }
        await context.SaveChangesAsync();
        
        var evidence = await context.Evidences.FindAsync(evidenceId);
        var result = mapper.Map<EvidenceCaseDto>(evidence!);
        result.EvidenceId = evidenceId;
        result.CaseId = caseEvidence.CaseId;
        result.ApprovalStatus = caseEvidence.ApprovalStatus;
        return result;
    }
    #endregion

    #region Suspect
    public async Task<SuspectDto> CreateSuspectAsync(int caseId, CreateSuspectDto dto, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
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
        result.ApprovalStatus = ApprovalStatus.Draft;
        return result;
    }

    public async Task<SuspectDto> UpdateSuspectAsync(int suspectId, UpdateSuspectDto dto, string detectiveIdentity)
    {
        await EnsureSuspectEditableByDetectiveAsync(detectiveIdentity, suspectId);
        var suspect = await context.Suspects.FindAsync(suspectId)
            ?? throw new EntityNotFoundException("Suspect", suspectId);
        
        var caseSuspects = await context.CaseSuspects
            .Where(cs => cs.SuspectId == suspectId)
            .ToListAsync();
        var caseSuspect = caseSuspects
            .OrderBy(cs => cs.CaseId)
            .FirstOrDefault()
            ?? throw new EntityNotFoundException("CaseSuspect", suspectId);

        mapper.Map(dto, suspect);
        foreach (var cs in caseSuspects)
        {
            cs.ApprovalStatus = ApprovalStatus.Draft;
        }
        await context.SaveChangesAsync();

        var result = mapper.Map<SuspectDto>(suspect);
        result.ApprovalStatus = caseSuspect.ApprovalStatus;
        return result;
    }

    public async Task DeleteSuspectAsync(int suspectId, string detectiveIdentity)
    {
        await EnsureSuspectOnlyLinkedToAllowedCasesAsync(detectiveIdentity, suspectId);
        var suspect = await context.Suspects.FindAsync(suspectId)
            ?? throw new EntityNotFoundException("Suspect", suspectId);

        var links = await context.CaseSuspects
            .Where(cs => cs.SuspectId == suspectId)
            .ToListAsync();
        context.CaseSuspects.RemoveRange(links);
        context.Suspects.Remove(suspect);
        await context.SaveChangesAsync();
    }

    public async Task<SuspectDto?> GetSuspectAsync(int suspectId)
    {
        var suspect = await context.Suspects.FindAsync(suspectId);
        if (suspect == null) return null;
        
        var caseSuspect = await context.CaseSuspects
            .FirstOrDefaultAsync(cs => cs.SuspectId == suspectId);
        
        var result = mapper.Map<SuspectDto>(suspect);
        if (caseSuspect != null)
        {
            result.ApprovalStatus = caseSuspect.ApprovalStatus;
        }
        return result;
    }

    public async Task<List<SuspectDto>> GetSuspectsAsync()
    {
        var suspects = await context.Suspects.ToListAsync();
        var caseSuspects = await context.CaseSuspects.ToListAsync();
        
        var results = suspects.Select(s =>
        {
            var dto = mapper.Map<SuspectDto>(s);
            var cs = caseSuspects.FirstOrDefault(cse => cse.SuspectId == s.Id);
            if (cs != null)
            {
                dto.ApprovalStatus = cs.ApprovalStatus;
            }
            return dto;
        }).ToList();
        
        return results;
    }

    public async Task<List<SuspectDto>> GetSuspectsFromCase(int caseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        return await GetSuspectsFromCaseInternalAsync(caseId);
    }

    private async Task<List<SuspectDto>> GetSuspectsFromCaseInternalAsync(int caseId) =>
        await context.CaseSuspects
            .Where(cs => cs.CaseId == caseId)
            .Select(cs => new SuspectDto
            {
                Id = cs.Suspect.Id,
                CaseId = cs.CaseId,
                FirstName = cs.Suspect.FirstName,
                LastName = cs.Suspect.LastName,
                FatherName = cs.Suspect.FatherName,
                Nickname = cs.Suspect.Nickname,
                PhoneNumber = cs.Suspect.PhoneNumber,
                DateOfBirth = cs.Suspect.DateOfBirth,
                Region = cs.Suspect.Region,
                City = cs.Suspect.City,
                Street = cs.Suspect.Street,
                BuildingNumber = cs.Suspect.BuildingNumber,
                ApartmentNumber = cs.Suspect.ApartmentNumber,
                Height = cs.Suspect.Height,
                Weight = cs.Suspect.Weight,
                PhysicalDescription = cs.Suspect.PhysicalDescription,
                PriorConvictions = cs.Suspect.PriorConvictions,
                ApprovalStatus = cs.ApprovalStatus
            })
            .ToListAsync();

    public async Task<List<SuspectDto>> GetApprovedSuspectsAsync(string detectiveIdentity)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return await context.CaseSuspects
            .Where(cs => allowed.Contains(cs.CaseId) && cs.ApprovalStatus == ApprovalStatus.Approved)
            .Select(cs => mapper.Map<SuspectDto>(cs.Suspect))
            .ToListAsync();
    }

    public async Task<List<SuspectDto>> GetDeclinedSuspectsAsync(string detectiveIdentity)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return await context.CaseSuspects
            .Where(cs => allowed.Contains(cs.CaseId) && cs.ApprovalStatus == ApprovalStatus.Declined)
            .Select(cs => mapper.Map<SuspectDto>(cs.Suspect))
            .ToListAsync();
    }

    public async Task<List<SuspectDto>> GetPendingSuspectsAsync(string detectiveIdentity)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return await context.CaseSuspects
            .Where(cs => allowed.Contains(cs.CaseId) && cs.ApprovalStatus == ApprovalStatus.Pending)
            .Select(cs => mapper.Map<SuspectDto>(cs.Suspect))
            .ToListAsync();
    }

    public async Task LinkSuspectToCaseAsync(int suspectId, int caseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        var entity = new CaseSuspect
        {
            CaseId = caseId,
            SuspectId = suspectId,
            ApprovalStatus = ApprovalStatus.Draft
        };
        context.CaseSuspects.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UnlinkSuspectFromCaseAsync(int suspectId, int caseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        var entity = await context.CaseSuspects
            .FirstOrDefaultAsync(cs => cs.SuspectId == suspectId && cs.CaseId == caseId)
            ?? throw new InvalidOperationException("Suspect not linked to case");

        context.CaseSuspects.Remove(entity);
        await context.SaveChangesAsync();
    }

    public async Task<SuspectDto> SubmitSuspectAsync(int suspectId, string detectiveIdentity)
    {
        await EnsureSuspectOnlyLinkedToAllowedCasesAsync(detectiveIdentity, suspectId);
        var caseSuspects = await context.CaseSuspects
            .Where(cs => cs.SuspectId == suspectId)
            .ToListAsync();
        var caseSuspect = caseSuspects
            .OrderBy(cs => cs.CaseId)
            .FirstOrDefault()
            ?? throw new EntityNotFoundException("CaseSuspect", suspectId);

        foreach (var cs in caseSuspects)
        {
            cs.ApprovalStatus = ApprovalStatus.Pending;
        }
        await context.SaveChangesAsync();
        
        var suspect = await context.Suspects.FindAsync(suspectId);
        var result = mapper.Map<SuspectDto>(suspect!);
        result.ApprovalStatus = caseSuspect.ApprovalStatus;
        return result;
    }
    #endregion

    #region Expense
    public async Task<ExpenseDto> CreateExpenseAsync(int caseId, CreateExpenseDto dto, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        var expense = mapper.Map<Expense>(dto);
        expense.CaseId = caseId;
        expense.ApprovalStatus = ApprovalStatus.Draft;
        context.Expenses.Add(expense);
        await context.SaveChangesAsync();
        return mapper.Map<ExpenseDto>(expense);
    }

    public async Task<ExpenseDto> UpdateExpenseAsync(int expenseId, UpdateExpenseDto dto, string detectiveIdentity)
    {
        await EnsureExpenseAllowedForDetectiveAsync(detectiveIdentity, expenseId);
        var expense = await context.Expenses.FindAsync(expenseId)
            ?? throw new EntityNotFoundException("Expense", expenseId);

        mapper.Map(dto, expense);
        // Set status to Draft after edit
        expense.ApprovalStatus = ApprovalStatus.Draft;
        await context.SaveChangesAsync();
        return mapper.Map<ExpenseDto>(expense);
    }

    public async Task DeleteExpenseAsync(int expenseId, string detectiveIdentity)
    {
        await EnsureExpenseAllowedForDetectiveAsync(detectiveIdentity, expenseId);
        var expense = await context.Expenses.FindAsync(expenseId)
            ?? throw new EntityNotFoundException("Expense", expenseId);

        context.Expenses.Remove(expense);
        await context.SaveChangesAsync();
    }

    public async Task<ExpenseDto?> GetExpenseAsync(int expenseId, string detectiveIdentity)
    {
        var expense = await context.Expenses
            .Where(e => e.Id == expenseId)
            .ProjectTo<ExpenseDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
        if (expense == null)
            return null;
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return allowed.Contains(expense.CaseId) ? expense : null;
    }

    public async Task<List<ExpenseDto>> GetExpensesAsync(string detectiveIdentity)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return await context.Expenses
            .Where(e => allowed.Contains(e.CaseId))
            .ProjectTo<ExpenseDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<ExpenseDto>> GetExpensesFromCaseAsync(int caseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        return await context.Expenses
            .Where(e => e.CaseId == caseId)
            .ProjectTo<ExpenseDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<ExpenseDto>> GetApprovedExpensesAsync(string detectiveIdentity)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return await context.Expenses
            .Where(e => allowed.Contains(e.CaseId) && e.ApprovalStatus == ApprovalStatus.Approved)
            .ProjectTo<ExpenseDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<ExpenseDto>> GetDeclinedExpensesAsync(string detectiveIdentity)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return await context.Expenses
            .Where(e => allowed.Contains(e.CaseId) && e.ApprovalStatus == ApprovalStatus.Declined)
            .ProjectTo<ExpenseDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<ExpenseDto>> GetPendingExpensesAsync(string detectiveIdentity)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return await context.Expenses
            .Where(e => allowed.Contains(e.CaseId) && e.ApprovalStatus == ApprovalStatus.Pending)
            .ProjectTo<ExpenseDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }
    #endregion

    #region Report
    public async Task<ReportDto> CreateReportAsync(int caseId, CreateReportDto dto, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        var report = mapper.Map<Report>(dto);
        report.CaseId = caseId;
        report.ApprovalStatus = ApprovalStatus.Draft;
        context.Reports.Add(report);
        await context.SaveChangesAsync();
        return mapper.Map<ReportDto>(report);
    }

    public async Task<ReportDto> UpdateReportAsync(int reportId, UpdateReportDto dto, string detectiveIdentity)
    {
        await EnsureReportAllowedForDetectiveAsync(detectiveIdentity, reportId);
        var report = await context.Reports.FindAsync(reportId)
            ?? throw new EntityNotFoundException("Report", reportId);

        mapper.Map(dto, report);
        // Set status to Draft after edit
        report.ApprovalStatus = ApprovalStatus.Draft;
        await context.SaveChangesAsync();
        return mapper.Map<ReportDto>(report);
    }

    public async Task DeleteReportAsync(int reportId, string detectiveIdentity)
    {
        await EnsureReportAllowedForDetectiveAsync(detectiveIdentity, reportId);
        var report = await context.Reports.FindAsync(reportId)
            ?? throw new EntityNotFoundException("Report", reportId);

        context.Reports.Remove(report);
        await context.SaveChangesAsync();
    }

    public async Task<ReportDto?> GetReportAsync(int reportId, string detectiveIdentity)
    {
        var report = await context.Reports
            .Where(r => r.Id == reportId)
            .ProjectTo<ReportDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
        if (report == null)
            return null;
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return allowed.Contains(report.CaseId) ? report : null;
    }

    public async Task<List<ReportDto>> GetReportsAsync(string detectiveIdentity)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return await context.Reports
            .Where(r => allowed.Contains(r.CaseId))
            .ProjectTo<ReportDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<ReportDto>> GetReportsFromCaseAsync(int caseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        return await context.Reports
            .Where(r => r.CaseId == caseId)
            .ProjectTo<ReportDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<ReportDto>> GetApprovedReportsAsync(string detectiveIdentity)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return await context.Reports
            .Where(r => allowed.Contains(r.CaseId) && r.ApprovalStatus == ApprovalStatus.Approved)
            .ProjectTo<ReportDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<ReportDto>> GetDeclinedReportsAsync(string detectiveIdentity)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return await context.Reports
            .Where(r => allowed.Contains(r.CaseId) && r.ApprovalStatus == ApprovalStatus.Declined)
            .ProjectTo<ReportDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<ReportDto>> GetPendingReportsAsync(string detectiveIdentity)
    {
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        return await context.Reports
            .Where(r => allowed.Contains(r.CaseId) && r.ApprovalStatus == ApprovalStatus.Pending)
            .ProjectTo<ReportDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<ReportDto> SubmitReportAsync(int reportId, string detectiveIdentity)
    {
        await EnsureReportAllowedForDetectiveAsync(detectiveIdentity, reportId);
        var report = await context.Reports.FindAsync(reportId)
            ?? throw new EntityNotFoundException("Report", reportId);

        report.ApprovalStatus = ApprovalStatus.Pending;
        await context.SaveChangesAsync();
        return mapper.Map<ReportDto>(report);
    }

    public async Task<ExpenseDto> SubmitExpenseAsync(int expenseId, string detectiveIdentity)
    {
        await EnsureExpenseAllowedForDetectiveAsync(detectiveIdentity, expenseId);
        var expense = await context.Expenses.FindAsync(expenseId)
            ?? throw new EntityNotFoundException("Expense", expenseId);

        expense.ApprovalStatus = ApprovalStatus.Pending;
        await context.SaveChangesAsync();
        return mapper.Map<ExpenseDto>(expense);
    }
    #endregion
}
