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

    private async Task<int?> GetDetectiveIdForIdentityAsync(string identity)
    {
        var d = await FindDetectiveByIdentityAsync(identity);
        return d?.Id;
    }

    private static bool DetectiveOwnsEntity(int? createdByDetectiveId, int detectiveId) =>
        createdByDetectiveId == detectiveId;

    private static bool IsDraftOrDeclined(ApprovalStatus s) =>
        s is ApprovalStatus.Draft or ApprovalStatus.Declined;

    /// <summary>Approved for everyone; otherwise any row created by this detective (any approval status).</summary>
    private static bool EvidenceCatalogVisible(Evidence e, int detectiveId) =>
        e.ApprovalStatus == ApprovalStatus.Approved
        || DetectiveOwnsEntity(e.CreatedByDetectiveId, detectiveId);

    /// <summary>Approved for everyone; otherwise any row created by this detective (any approval status).</summary>
    private static bool SuspectCatalogVisible(Suspect s, int detectiveId) =>
        s.ApprovalStatus == ApprovalStatus.Approved
        || DetectiveOwnsEntity(s.CreatedByDetectiveId, detectiveId);

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

    private async Task EnsureDetectiveOwnsDraftOrDeclinedExpenseAsync(string identity, Expense expense)
    {
        var detId = await GetDetectiveIdForIdentityAsync(identity)
                    ?? throw new EntityNotFoundException("Expense", expense.Id);
        if (!IsDraftOrDeclined(expense.ApprovalStatus))
            throw new EntityNotFoundException("Expense", expense.Id);
        if (expense.CreatedByDetectiveId.HasValue && expense.CreatedByDetectiveId.Value != detId)
            throw new EntityNotFoundException("Expense", expense.Id);
    }

    private async Task EnsureDetectiveOwnsDraftOrDeclinedReportAsync(string identity, Report report)
    {
        var detId = await GetDetectiveIdForIdentityAsync(identity)
                    ?? throw new EntityNotFoundException("Report", report.Id);
        if (!IsDraftOrDeclined(report.ApprovalStatus))
            throw new EntityNotFoundException("Report", report.Id);
        if (report.CreatedByDetectiveId.HasValue && report.CreatedByDetectiveId.Value != detId)
            throw new EntityNotFoundException("Report", report.Id);
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

    /// <summary>Edit own draft/declined evidence; if linked, at least one link must be to an allowed case.</summary>
    private async Task EnsureEvidenceEditableByDetectiveAsync(string identity, int evidenceId)
    {
        var detId = await GetDetectiveIdForIdentityAsync(identity)
                    ?? throw new EntityNotFoundException("Evidence", evidenceId);
        var evidence = await context.Evidences.FirstOrDefaultAsync(e => e.Id == evidenceId)
                       ?? throw new EntityNotFoundException("Evidence", evidenceId);
        if (!IsDraftOrDeclined(evidence.ApprovalStatus))
            throw new EntityNotFoundException("Evidence", evidenceId);
        if (evidence.CreatedByDetectiveId.HasValue && evidence.CreatedByDetectiveId != detId)
            throw new EntityNotFoundException("Evidence", evidenceId);

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
        var detId = await GetDetectiveIdForIdentityAsync(identity)
                    ?? throw new EntityNotFoundException("Suspect", suspectId);
        var suspect = await context.Suspects.FirstOrDefaultAsync(s => s.Id == suspectId)
                      ?? throw new EntityNotFoundException("Suspect", suspectId);
        if (!IsDraftOrDeclined(suspect.ApprovalStatus))
            throw new EntityNotFoundException("Suspect", suspectId);
        if (suspect.CreatedByDetectiveId.HasValue && suspect.CreatedByDetectiveId != detId)
            throw new EntityNotFoundException("Suspect", suspectId);

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
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity)
                    ?? throw new EntityNotFoundException("Detective", 0);

        var evidenceEntity = mapper.Map<Evidence>(dto);
        evidenceEntity.ApprovalStatus = ApprovalStatus.Draft;
        evidenceEntity.CreatedByDetectiveId = detId;
        context.Evidences.Add(evidenceEntity);

        var caseEvidenceEntity = new CaseEvidence
        {
            CaseId = caseId,
            Evidence = evidenceEntity,
        };

        context.CaseEvidences.Add(caseEvidenceEntity);
        await context.SaveChangesAsync();

        var result = mapper.Map<EvidenceCaseDto>(evidenceEntity);
        result.CaseId = caseId;
        result.EvidenceId = evidenceEntity.Id;
        result.ApprovalStatus = evidenceEntity.ApprovalStatus;
        return result;
    }

    public async Task<EvidenceCaseDto> UpdateEvidenceAsync(int evidenceId, UpdateEvidenceDto dto, string detectiveIdentity)
    {
        await EnsureEvidenceEditableByDetectiveAsync(detectiveIdentity, evidenceId);
        var evidence = await context.Evidences.FindAsync(evidenceId)
            ?? throw new EntityNotFoundException("Evidence", evidenceId);

        var caseEvidence = await context.CaseEvidences
            .Where(ce => ce.EvidenceId == evidenceId)
            .OrderBy(ce => ce.CaseId)
            .FirstOrDefaultAsync();

        mapper.Map(dto, evidence);
        evidence.ApprovalStatus = ApprovalStatus.Draft;
        await context.SaveChangesAsync();

        var result = mapper.Map<EvidenceCaseDto>(evidence);
        result.EvidenceId = evidence.Id;
        result.CaseId = caseEvidence?.CaseId;
        result.ApprovalStatus = evidence.ApprovalStatus;
        return result;
    }

    public async Task DeleteEvidenceAsync(int evidenceId, string detectiveIdentity)
    {
        await EnsureEvidenceOnlyLinkedToAllowedCasesAsync(detectiveIdentity, evidenceId);
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity)
                    ?? throw new EntityNotFoundException("Evidence", evidenceId);
        var evidence = await context.Evidences.FindAsync(evidenceId)
                       ?? throw new EntityNotFoundException("Evidence", evidenceId);
        if (!IsDraftOrDeclined(evidence.ApprovalStatus))
            throw new EntityNotFoundException("Evidence", evidenceId);
        if (evidence.CreatedByDetectiveId.HasValue && evidence.CreatedByDetectiveId != detId)
            throw new EntityNotFoundException("Evidence", evidenceId);

        var links = await context.CaseEvidences
            .Where(ce => ce.EvidenceId == evidenceId)
            .ToListAsync();
        context.CaseEvidences.RemoveRange(links);
        context.Evidences.Remove(evidence);
        await context.SaveChangesAsync();
    }

    private async Task<bool> EvidenceRowVisibleToDetectiveAsync(int evidenceId, string detectiveIdentity)
    {
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity);
        if (detId == null) return false;
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        var e = await context.Evidences.AsNoTracking().FirstOrDefaultAsync(x => x.Id == evidenceId);
        if (e == null) return false;
        if (EvidenceCatalogVisible(e, detId.Value))
            return true;
        return await context.CaseEvidences.AnyAsync(ce => ce.EvidenceId == evidenceId && allowed.Contains(ce.CaseId));
    }

    public async Task<EvidenceCaseDto?> GetEvidenceAsync(int evidenceId, string detectiveIdentity)
    {
        if (!await EvidenceRowVisibleToDetectiveAsync(evidenceId, detectiveIdentity))
            return null;

        var e = await context.Evidences.AsNoTracking().FirstAsync(x => x.Id == evidenceId);
        var caseId = await context.CaseEvidences
            .Where(ce => ce.EvidenceId == evidenceId)
            .OrderBy(ce => ce.CaseId)
            .Select(ce => (int?)ce.CaseId)
            .FirstOrDefaultAsync();

        return new EvidenceCaseDto
        {
            EvidenceId = e.Id,
            CaseId = caseId,
            Type = e.Type,
            Description = e.Description,
            CollectionDate = e.CollectionDate,
            Region = e.Region,
            Annotation = e.Annotation,
            Purpose = e.Purpose,
            ApprovalStatus = e.ApprovalStatus
        };
    }

    /// <summary>All approved evidences plus any row created by this detective (any status). <see cref="EvidenceCaseDto.CaseId"/> is a representative link if any.</summary>
    public async Task<List<EvidenceCaseDto>> GetEvidencesAsync(string detectiveIdentity)
    {
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity);
        if (detId == null)
            return [];

        var evidences = await context.Evidences.AsNoTracking()
            .Where(e => e.ApprovalStatus == ApprovalStatus.Approved || e.CreatedByDetectiveId == detId)
            .OrderBy(e => e.Id)
            .ToListAsync();

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
                ApprovalStatus = e.ApprovalStatus
            };
        }).ToList();
    }

    /// <summary>Evidences this detective may attach to a case (approved pool + own workflow rows).</summary>
    public async Task<List<EvidenceCaseDto>> GetEvidencesLinkableToCaseAsync(int caseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        return await GetEvidencesAsync(detectiveIdentity);
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
                ApprovalStatus = ce.Evidence.ApprovalStatus
            })
            .ToListAsync();

    public async Task<List<EvidenceCaseDto>> GetApprovedEvidencesAsync(string detectiveIdentity)
    {
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity);
        if (detId == null)
            return [];

        var evidences = await context.Evidences.AsNoTracking()
            .Where(e => e.ApprovalStatus == ApprovalStatus.Approved)
            .OrderBy(e => e.Id)
            .ToListAsync();
        return await MapEvidenceDtosWithRepresentativeCase(evidences);
    }

    public async Task<List<EvidenceCaseDto>> GetDeclinedEvidencesAsync(string detectiveIdentity)
    {
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity);
        if (detId == null)
            return [];

        var evidences = await context.Evidences.AsNoTracking()
            .Where(e => e.CreatedByDetectiveId == detId && e.ApprovalStatus == ApprovalStatus.Declined)
            .OrderBy(e => e.Id)
            .ToListAsync();
        return await MapEvidenceDtosWithRepresentativeCase(evidences);
    }

    public async Task<List<EvidenceCaseDto>> GetPendingEvidencesAsync(string detectiveIdentity)
    {
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity);
        if (detId == null)
            return [];

        var evidences = await context.Evidences.AsNoTracking()
            .Where(e => e.CreatedByDetectiveId == detId && e.ApprovalStatus == ApprovalStatus.Pending)
            .OrderBy(e => e.Id)
            .ToListAsync();
        return await MapEvidenceDtosWithRepresentativeCase(evidences);
    }

    private async Task<List<EvidenceCaseDto>> MapEvidenceDtosWithRepresentativeCase(List<Evidence> evidences)
    {
        if (evidences.Count == 0)
            return [];
        var ids = evidences.Select(e => e.Id).ToList();
        var links = await context.CaseEvidences.AsNoTracking()
            .Where(ce => ids.Contains(ce.EvidenceId))
            .ToListAsync();
        var firstByEvidence = links
            .GroupBy(ce => ce.EvidenceId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.CaseId).First().CaseId);

        return evidences.Select(e => new EvidenceCaseDto
        {
            EvidenceId = e.Id,
            CaseId = firstByEvidence.TryGetValue(e.Id, out var cid) ? cid : null,
            Type = e.Type,
            Description = e.Description,
            CollectionDate = e.CollectionDate,
            Region = e.Region,
            Annotation = e.Annotation,
            Purpose = e.Purpose,
            ApprovalStatus = e.ApprovalStatus
        }).ToList();
    }

    private async Task EnsureDetectiveMayLinkEvidenceAsync(int evidenceId, string detectiveIdentity)
    {
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity)
                    ?? throw new EntityNotFoundException("Evidence", evidenceId);
        var evidence = await context.Evidences.AsNoTracking().FirstOrDefaultAsync(e => e.Id == evidenceId)
                       ?? throw new EntityNotFoundException("Evidence", evidenceId);
        if (evidence.ApprovalStatus == ApprovalStatus.Approved)
            return;
        if (DetectiveOwnsEntity(evidence.CreatedByDetectiveId, detId))
            return;
        throw new EntityNotFoundException("Evidence", evidenceId);
    }

    public async Task LinkEvidenceToCaseAsync(int evidenceId, int caseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        await EnsureDetectiveMayLinkEvidenceAsync(evidenceId, detectiveIdentity);
        if (await context.CaseEvidences.AnyAsync(ce => ce.EvidenceId == evidenceId && ce.CaseId == caseId))
            return;

        context.CaseEvidences.Add(new CaseEvidence { CaseId = caseId, EvidenceId = evidenceId });
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
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity)
                    ?? throw new EntityNotFoundException("Evidence", evidenceId);
        var evidence = await context.Evidences.FindAsync(evidenceId)
                       ?? throw new EntityNotFoundException("Evidence", evidenceId);
        if (evidence.ApprovalStatus != ApprovalStatus.Draft)
            throw new EntityNotFoundException("Evidence", evidenceId);
        if (evidence.CreatedByDetectiveId.HasValue && evidence.CreatedByDetectiveId.Value != detId)
            throw new EntityNotFoundException("Evidence", evidenceId);

        evidence.ApprovalStatus = ApprovalStatus.Pending;
        await context.SaveChangesAsync();

        var caseId = await context.CaseEvidences
            .Where(ce => ce.EvidenceId == evidenceId)
            .OrderBy(ce => ce.CaseId)
            .Select(ce => (int?)ce.CaseId)
            .FirstOrDefaultAsync();

        var result = mapper.Map<EvidenceCaseDto>(evidence);
        result.EvidenceId = evidenceId;
        result.CaseId = caseId;
        result.ApprovalStatus = evidence.ApprovalStatus;
        return result;
    }
    #endregion

    #region Suspect
    public async Task<SuspectDto> CreateSuspectAsync(int caseId, CreateSuspectDto dto, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity)
                    ?? throw new EntityNotFoundException("Detective", 0);

        var suspect = mapper.Map<Suspect>(dto);
        suspect.ApprovalStatus = ApprovalStatus.Draft;
        suspect.CreatedByDetectiveId = detId;
        context.Suspects.Add(suspect);

        context.CaseSuspects.Add(new CaseSuspect
        {
            CaseId = caseId,
            Suspect = suspect,
            IsInterrogated = false,
        });

        await context.SaveChangesAsync();

        var result = mapper.Map<SuspectDto>(suspect);
        result.CaseId = caseId;
        result.ApprovalStatus = suspect.ApprovalStatus;
        return result;
    }

    public async Task<SuspectDto> UpdateSuspectAsync(int suspectId, UpdateSuspectDto dto, string detectiveIdentity)
    {
        await EnsureSuspectEditableByDetectiveAsync(detectiveIdentity, suspectId);
        var suspect = await context.Suspects.FindAsync(suspectId)
                      ?? throw new EntityNotFoundException("Suspect", suspectId);

        var caseId = await context.CaseSuspects
            .Where(cs => cs.SuspectId == suspectId)
            .OrderBy(cs => cs.CaseId)
            .Select(cs => (int?)cs.CaseId)
            .FirstOrDefaultAsync();

        mapper.Map(dto, suspect);
        suspect.ApprovalStatus = ApprovalStatus.Draft;
        await context.SaveChangesAsync();

        var result = mapper.Map<SuspectDto>(suspect);
        result.CaseId = caseId;
        result.ApprovalStatus = suspect.ApprovalStatus;
        return result;
    }

    public async Task DeleteSuspectAsync(int suspectId, string detectiveIdentity)
    {
        await EnsureSuspectOnlyLinkedToAllowedCasesAsync(detectiveIdentity, suspectId);
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity)
                    ?? throw new EntityNotFoundException("Suspect", suspectId);
        var suspect = await context.Suspects.FindAsync(suspectId)
                      ?? throw new EntityNotFoundException("Suspect", suspectId);
        if (!IsDraftOrDeclined(suspect.ApprovalStatus))
            throw new EntityNotFoundException("Suspect", suspectId);
        if (suspect.CreatedByDetectiveId.HasValue && suspect.CreatedByDetectiveId.Value != detId)
            throw new EntityNotFoundException("Suspect", suspectId);

        var links = await context.CaseSuspects
            .Where(cs => cs.SuspectId == suspectId)
            .ToListAsync();
        context.CaseSuspects.RemoveRange(links);
        context.Suspects.Remove(suspect);
        await context.SaveChangesAsync();
    }

    private async Task<bool> SuspectRowVisibleToDetectiveAsync(int suspectId, string detectiveIdentity)
    {
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity);
        if (detId == null) return false;
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        var s = await context.Suspects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == suspectId);
        if (s == null) return false;
        if (SuspectCatalogVisible(s, detId.Value))
            return true;
        return await context.CaseSuspects.AnyAsync(cs => cs.SuspectId == suspectId && allowed.Contains(cs.CaseId));
    }

    public async Task<SuspectDto?> GetSuspectAsync(int suspectId, string detectiveIdentity)
    {
        if (!await SuspectRowVisibleToDetectiveAsync(suspectId, detectiveIdentity))
            return null;

        var suspect = await context.Suspects.FindAsync(suspectId);
        if (suspect == null) return null;

        var caseId = await context.CaseSuspects
            .Where(cs => cs.SuspectId == suspectId)
            .OrderBy(cs => cs.CaseId)
            .Select(cs => (int?)cs.CaseId)
            .FirstOrDefaultAsync();

        var result = mapper.Map<SuspectDto>(suspect);
        result.CaseId = caseId;
        result.ApprovalStatus = suspect.ApprovalStatus;
        return result;
    }

    public async Task<List<SuspectDto>> GetSuspectsAsync(string detectiveIdentity)
    {
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity);
        if (detId == null)
            return [];

        var suspects = await context.Suspects.AsNoTracking()
            .Where(s => s.ApprovalStatus == ApprovalStatus.Approved || s.CreatedByDetectiveId == detId)
            .OrderBy(s => s.Id)
            .ToListAsync();

        return await MapSuspectDtosWithRepresentativeCase(suspects);
    }

    public async Task<List<SuspectDto>> GetSuspectsLinkableToCaseAsync(int caseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        return await GetSuspectsAsync(detectiveIdentity);
    }

    private async Task<List<SuspectDto>> MapSuspectDtosWithRepresentativeCase(List<Suspect> suspects)
    {
        if (suspects.Count == 0)
            return [];
        var ids = suspects.Select(s => s.Id).ToList();
        var links = await context.CaseSuspects.AsNoTracking()
            .Where(cs => ids.Contains(cs.SuspectId))
            .ToListAsync();
        var caseBySuspect = links
            .GroupBy(cs => cs.SuspectId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.CaseId).First().CaseId);

        return suspects.Select(s =>
        {
            var dto = mapper.Map<SuspectDto>(s);
            if (caseBySuspect.TryGetValue(s.Id, out var cid))
                dto.CaseId = cid;
            dto.ApprovalStatus = s.ApprovalStatus;
            return dto;
        }).ToList();
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
                ApprovalStatus = cs.Suspect.ApprovalStatus
            })
            .ToListAsync();

    public async Task<List<SuspectDto>> GetApprovedSuspectsAsync(string detectiveIdentity)
    {
        var suspects = await context.Suspects.AsNoTracking()
            .Where(s => s.ApprovalStatus == ApprovalStatus.Approved)
            .OrderBy(s => s.Id)
            .ToListAsync();
        return await MapSuspectDtosWithRepresentativeCase(suspects);
    }

    public async Task<List<SuspectDto>> GetDeclinedSuspectsAsync(string detectiveIdentity)
    {
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity);
        if (detId == null)
            return [];

        var suspects = await context.Suspects.AsNoTracking()
            .Where(s => s.CreatedByDetectiveId == detId && s.ApprovalStatus == ApprovalStatus.Declined)
            .OrderBy(s => s.Id)
            .ToListAsync();
        return await MapSuspectDtosWithRepresentativeCase(suspects);
    }

    public async Task<List<SuspectDto>> GetPendingSuspectsAsync(string detectiveIdentity)
    {
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity);
        if (detId == null)
            return [];

        var suspects = await context.Suspects.AsNoTracking()
            .Where(s => s.CreatedByDetectiveId == detId && s.ApprovalStatus == ApprovalStatus.Pending)
            .OrderBy(s => s.Id)
            .ToListAsync();
        return await MapSuspectDtosWithRepresentativeCase(suspects);
    }

    private async Task EnsureDetectiveMayLinkSuspectAsync(int suspectId, string detectiveIdentity)
    {
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity)
                    ?? throw new EntityNotFoundException("Suspect", suspectId);
        var suspect = await context.Suspects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == suspectId)
                      ?? throw new EntityNotFoundException("Suspect", suspectId);
        if (suspect.ApprovalStatus == ApprovalStatus.Approved)
            return;
        if (DetectiveOwnsEntity(suspect.CreatedByDetectiveId, detId))
            return;
        throw new EntityNotFoundException("Suspect", suspectId);
    }

    public async Task LinkSuspectToCaseAsync(int suspectId, int caseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        await EnsureDetectiveMayLinkSuspectAsync(suspectId, detectiveIdentity);
        if (await context.CaseSuspects.AnyAsync(cs => cs.SuspectId == suspectId && cs.CaseId == caseId))
            return;

        context.CaseSuspects.Add(new CaseSuspect
        {
            CaseId = caseId,
            SuspectId = suspectId,
            IsInterrogated = false,
        });
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
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity)
                    ?? throw new EntityNotFoundException("Suspect", suspectId);
        var suspect = await context.Suspects.FindAsync(suspectId)
                      ?? throw new EntityNotFoundException("Suspect", suspectId);
        if (suspect.ApprovalStatus != ApprovalStatus.Draft)
            throw new EntityNotFoundException("Suspect", suspectId);
        if (suspect.CreatedByDetectiveId.HasValue && suspect.CreatedByDetectiveId.Value != detId)
            throw new EntityNotFoundException("Suspect", suspectId);

        suspect.ApprovalStatus = ApprovalStatus.Pending;
        await context.SaveChangesAsync();

        var caseId = await context.CaseSuspects
            .Where(cs => cs.SuspectId == suspectId)
            .OrderBy(cs => cs.CaseId)
            .Select(cs => (int?)cs.CaseId)
            .FirstOrDefaultAsync();

        var result = mapper.Map<SuspectDto>(suspect);
        result.CaseId = caseId;
        result.ApprovalStatus = suspect.ApprovalStatus;
        return result;
    }
    #endregion

    #region Expense
    public async Task<ExpenseDto> CreateExpenseAsync(int caseId, CreateExpenseDto dto, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity)
                    ?? throw new EntityNotFoundException("Detective", 0);
        var expense = mapper.Map<Expense>(dto);
        expense.CaseId = caseId;
        expense.ApprovalStatus = ApprovalStatus.Draft;
        expense.CreatedByDetectiveId = detId;
        context.Expenses.Add(expense);
        await context.SaveChangesAsync();
        return mapper.Map<ExpenseDto>(expense);
    }

    public async Task<ExpenseDto> UpdateExpenseAsync(int expenseId, UpdateExpenseDto dto, string detectiveIdentity)
    {
        await EnsureExpenseAllowedForDetectiveAsync(detectiveIdentity, expenseId);
        var expense = await context.Expenses.FindAsync(expenseId)
            ?? throw new EntityNotFoundException("Expense", expenseId);
        await EnsureDetectiveOwnsDraftOrDeclinedExpenseAsync(detectiveIdentity, expense);

        mapper.Map(dto, expense);
        expense.ApprovalStatus = ApprovalStatus.Draft;
        await context.SaveChangesAsync();
        return mapper.Map<ExpenseDto>(expense);
    }

    public async Task DeleteExpenseAsync(int expenseId, string detectiveIdentity)
    {
        await EnsureExpenseAllowedForDetectiveAsync(detectiveIdentity, expenseId);
        var expense = await context.Expenses.FindAsync(expenseId)
            ?? throw new EntityNotFoundException("Expense", expenseId);
        await EnsureDetectiveOwnsDraftOrDeclinedExpenseAsync(detectiveIdentity, expense);

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

    public async Task<List<ExpenseDto>> GetExpensesAssignableToCaseAsync(int caseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity);
        if (detId == null)
            return [];

        return await context.Expenses
            .Where(e => e.CreatedByDetectiveId == detId
                        && e.CaseId != caseId
                        && allowed.Contains(e.CaseId)
                        && (e.ApprovalStatus == ApprovalStatus.Draft || e.ApprovalStatus == ApprovalStatus.Declined))
            .ProjectTo<ExpenseDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<ExpenseDto> AssignExpenseToCaseAsync(int expenseId, int newCaseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, newCaseId);
        await EnsureExpenseAllowedForDetectiveAsync(detectiveIdentity, expenseId);
        var expense = await context.Expenses.FindAsync(expenseId)
                      ?? throw new EntityNotFoundException("Expense", expenseId);
        await EnsureDetectiveOwnsDraftOrDeclinedExpenseAsync(detectiveIdentity, expense);

        expense.CaseId = newCaseId;
        await context.SaveChangesAsync();
        return mapper.Map<ExpenseDto>(expense);
    }
    #endregion

    #region Report
    public async Task<ReportDto> CreateReportAsync(int caseId, CreateReportDto dto, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity)
                    ?? throw new EntityNotFoundException("Detective", 0);
        var report = mapper.Map<Report>(dto);
        report.CaseId = caseId;
        report.ApprovalStatus = ApprovalStatus.Draft;
        report.CreatedByDetectiveId = detId;
        context.Reports.Add(report);
        await context.SaveChangesAsync();
        return mapper.Map<ReportDto>(report);
    }

    public async Task<ReportDto> UpdateReportAsync(int reportId, UpdateReportDto dto, string detectiveIdentity)
    {
        await EnsureReportAllowedForDetectiveAsync(detectiveIdentity, reportId);
        var report = await context.Reports.FindAsync(reportId)
            ?? throw new EntityNotFoundException("Report", reportId);
        await EnsureDetectiveOwnsDraftOrDeclinedReportAsync(detectiveIdentity, report);

        mapper.Map(dto, report);
        report.ApprovalStatus = ApprovalStatus.Draft;
        await context.SaveChangesAsync();
        return mapper.Map<ReportDto>(report);
    }

    public async Task DeleteReportAsync(int reportId, string detectiveIdentity)
    {
        await EnsureReportAllowedForDetectiveAsync(detectiveIdentity, reportId);
        var report = await context.Reports.FindAsync(reportId)
            ?? throw new EntityNotFoundException("Report", reportId);
        await EnsureDetectiveOwnsDraftOrDeclinedReportAsync(detectiveIdentity, report);

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

    public async Task<List<ReportDto>> GetReportsAssignableToCaseAsync(int caseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, caseId);
        var allowed = await GetAllowedCaseIdsForDetectiveIdentityAsync(detectiveIdentity);
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity);
        if (detId == null)
            return [];

        return await context.Reports
            .Where(r => r.CreatedByDetectiveId == detId
                        && r.CaseId != caseId
                        && allowed.Contains(r.CaseId)
                        && (r.ApprovalStatus == ApprovalStatus.Draft || r.ApprovalStatus == ApprovalStatus.Declined))
            .ProjectTo<ReportDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<ReportDto> AssignReportToCaseAsync(int reportId, int newCaseId, string detectiveIdentity)
    {
        await EnsureCaseAllowedForDetectiveAsync(detectiveIdentity, newCaseId);
        await EnsureReportAllowedForDetectiveAsync(detectiveIdentity, reportId);
        var report = await context.Reports.FindAsync(reportId)
                     ?? throw new EntityNotFoundException("Report", reportId);
        await EnsureDetectiveOwnsDraftOrDeclinedReportAsync(detectiveIdentity, report);

        report.CaseId = newCaseId;
        await context.SaveChangesAsync();
        return mapper.Map<ReportDto>(report);
    }

    public async Task<ReportDto> SubmitReportAsync(int reportId, string detectiveIdentity)
    {
        await EnsureReportAllowedForDetectiveAsync(detectiveIdentity, reportId);
        var report = await context.Reports.FindAsync(reportId)
            ?? throw new EntityNotFoundException("Report", reportId);
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity)
                    ?? throw new EntityNotFoundException("Report", reportId);
        if (report.ApprovalStatus != ApprovalStatus.Draft)
            throw new EntityNotFoundException("Report", reportId);
        if (report.CreatedByDetectiveId.HasValue && report.CreatedByDetectiveId.Value != detId)
            throw new EntityNotFoundException("Report", reportId);

        report.ApprovalStatus = ApprovalStatus.Pending;
        await context.SaveChangesAsync();
        return mapper.Map<ReportDto>(report);
    }

    public async Task<ExpenseDto> SubmitExpenseAsync(int expenseId, string detectiveIdentity)
    {
        await EnsureExpenseAllowedForDetectiveAsync(detectiveIdentity, expenseId);
        var expense = await context.Expenses.FindAsync(expenseId)
            ?? throw new EntityNotFoundException("Expense", expenseId);
        var detId = await GetDetectiveIdForIdentityAsync(detectiveIdentity)
                    ?? throw new EntityNotFoundException("Expense", expenseId);
        if (expense.ApprovalStatus != ApprovalStatus.Draft)
            throw new EntityNotFoundException("Expense", expenseId);
        if (expense.CreatedByDetectiveId.HasValue && expense.CreatedByDetectiveId.Value != detId)
            throw new EntityNotFoundException("Expense", expenseId);

        expense.ApprovalStatus = ApprovalStatus.Pending;
        await context.SaveChangesAsync();
        return mapper.Map<ExpenseDto>(expense);
    }
    #endregion
}
