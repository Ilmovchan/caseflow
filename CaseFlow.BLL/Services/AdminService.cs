using AutoMapper;
using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.CaseType;
using CaseFlow.BLL.Dto.Client;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Dto.Detective;
using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Exceptions;
using CaseFlow.DAL.Data;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.RegularExpressions;

namespace CaseFlow.BLL.Services;

/// <summary>Логіка адміна; при RLS з’єднання під PG-адміном. Чернетки приховуються в LINQ через обхід RLS власником таблиці.</summary>
public partial class AdminService(
    DetectiveAgencyDbContext context,
    IMapper mapper,
    IAdminRlsExecutionContext adminRls)
{
    public bool RowLevelSecurityAuthoritative => adminRls.RowLevelSecurityAdminPoliciesActive;

    private IQueryable<Evidence> EvidencesVisibleToAdmin =>
        context.Evidences.Where(e => e.ApprovalStatus != ApprovalStatus.Draft);

    private IQueryable<Suspect> SuspectsVisibleToAdmin =>
        context.Suspects.Where(s => s.ApprovalStatus != ApprovalStatus.Draft);

    private IQueryable<Expense> ExpensesVisibleToAdmin =>
        context.Expenses.Where(e => e.ApprovalStatus != ApprovalStatus.Draft);

    private IQueryable<Report> ReportsVisibleToAdmin =>
        context.Reports.Where(r => r.ApprovalStatus != ApprovalStatus.Draft);

    #region Справи

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
        var query = context.Cases
            .Include(c => c.CaseType)
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .AsQueryable();

        var allItems = await query.ToListAsync();
        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(c => c.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Case>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Case>> SearchCasesPagedAsync(string? searchTerm, int pageNumber = 1, int pageSize = 15)
    {
        var query = context.Cases
            .Include(c => c.CaseType)
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .AsQueryable();

        var allItems = await query.ToListAsync();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            allItems = allItems.Where(c =>
                c.Id.ToString().Contains(term) ||
                c.Title.ToLower().Contains(term) ||
                c.Status.ToString().ToLower().Contains(term) ||
                (c.Client != null && c.Client.FirstName.ToLower().Contains(term)) ||
                (c.Client != null && c.Client.LastName.ToLower().Contains(term)) ||
                (c.Detective != null && c.Detective.FirstName.ToLower().Contains(term)) ||
                (c.Detective != null && c.Detective.LastName.ToLower().Contains(term)) ||
                (c.CaseType != null && c.CaseType.Name.ToLower().Contains(term)) ||
                c.StartDate.ToString().Contains(term) ||
                c.DeadlineDate.ToString().Contains(term) ||
                (c.CloseDate != null && c.CloseDate.Value.ToString().Contains(term)))
                .ToList();
        }

        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(c => c.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

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
            throw new ArgumentException($"Клієнт з id {dto.ClientId} не існує");

        if (!await context.CaseTypes.AnyAsync(ct => ct.Id == dto.CaseTypeId))
            throw new ArgumentException($"Тип справи з id {dto.CaseTypeId} не існує");

        if (dto.DetectiveId.HasValue &&
            !await context.Detectives.AnyAsync(d => d.Id == dto.DetectiveId.Value))
            throw new ArgumentException($"Детектив з id {dto.DetectiveId} не існує");

        var caseEntity = mapper.Map<Case>(dto);
        context.Cases.Add(caseEntity);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                              pg.SqlState == "23505" &&
                                              string.Equals(pg.ConstraintName, "PK_case", StringComparison.OrdinalIgnoreCase))
        {
            await ReseedCaseIdSequenceAsync();
            await context.SaveChangesAsync();
        }

        return caseEntity;
    }

    private Task ReseedCaseIdSequenceAsync()
    {
        return context.Database.ExecuteSqlRawAsync(@"
DO $$
DECLARE seq text;
BEGIN
    seq := pg_get_serial_sequence('case', 'id');
    IF seq IS NOT NULL THEN
        PERFORM setval(
            seq::regclass,
            COALESCE((SELECT MAX(id) FROM ""case""), 0),
            true
        );
    END IF;
END $$;
");
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

    #region Клієнти

    public async Task<Client?> GetClientAsync(int clientId) =>
        await context.Clients.FindAsync(clientId);

    public async Task<List<Client>> GetClientsAsync() =>
        await context.Clients.ToListAsync();

    public async Task<PagedResult<Client>> GetClientsPagedAsync(int pageNumber = 1, int pageSize = 15)
    {
        var query = context.Clients.AsQueryable();
        var allItems = await query.ToListAsync();

        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(c => c.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Client>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Client>> SearchClientsPagedAsync(string? searchTerm, int pageNumber = 1, int pageSize = 15)
    {
        var query = context.Clients.AsQueryable();
        var allItems = await query.ToListAsync();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            allItems = allItems.Where(c =>
                c.Id.ToString().Contains(term) ||
                c.FirstName.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term) ||
                (c.FatherName != null && c.FatherName.ToLower().Contains(term)) ||
                (c.Email != null && c.Email.ToLower().Contains(term)) ||
                (c.PhoneNumber != null && c.PhoneNumber.Contains(term)) ||
                (c.City != null && c.City.ToLower().Contains(term)) ||
                (c.Region != null && c.Region.ToLower().Contains(term)) ||
                c.RegistrationDate.ToString().Contains(term))
                .ToList();
        }

        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(c => c.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

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

    #region Детективи

    public async Task<Detective?> GetDetectiveAsync(int detectiveId) =>
        await context.Detectives.FindAsync(detectiveId);

    public async Task<List<Detective>> GetDetectivesAsync() =>
        await context.Detectives.ToListAsync();

    public async Task<PagedResult<Detective>> GetDetectivesPagedAsync(int pageNumber = 1, int pageSize = 15)
    {
        var query = context.Detectives.AsQueryable();
        var allItems = await query.ToListAsync();

        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(d => d.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Detective>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Detective>> SearchDetectivesPagedAsync(string? searchTerm, int pageNumber = 1, int pageSize = 15)
    {
        var query = context.Detectives.AsQueryable();
        var allItems = await query.ToListAsync();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            allItems = allItems.Where(d =>
                d.Id.ToString().Contains(term) ||
                d.FirstName.ToLower().Contains(term) ||
                d.LastName.ToLower().Contains(term) ||
                (d.FatherName != null && d.FatherName.ToLower().Contains(term)) ||
                d.Status.ToString().ToLower().Contains(term) ||
                d.HireDate.ToString().Contains(term) ||
                (d.Email != null && d.Email.ToLower().Contains(term)) ||
                (d.PhoneNumber != null && d.PhoneNumber.Contains(term)) ||
                (d.City != null && d.City.ToLower().Contains(term)))
                .ToList();
        }

        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(d => d.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

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
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                              pg.SqlState == "23505" &&
                                              string.Equals(pg.ConstraintName, "PK_detective", StringComparison.OrdinalIgnoreCase))
        {
            await ReseedDetectiveIdSequenceAsync();
            await context.SaveChangesAsync();
        }

        return detectiveEntity;
    }

    public async Task<List<Detective>> GetDetectivesWithoutAccountsAsync() =>
        await context.Detectives
            .Where(d => d.PostgresLogin == null)
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToListAsync();

    public async Task<DetectiveAccountCreatedDto> CreateDetectiveAccountAsync(int detectiveId, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Потрібне ім’я користувача");
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Потрібен пароль");
        if (password.Length < 6)
            throw new ArgumentException("Пароль має бути не коротший за 6 символів");

        username = username.Trim();
        if (!Regex.IsMatch(username, "^[a-zA-Z0-9_]+$"))
            throw new ArgumentException("Логін: лише латинські літери, цифри та підкреслення");

        var detective = await context.Detectives.FindAsync(detectiveId)
            ?? throw new EntityNotFoundException("Detective", detectiveId);

        if (detective.PostgresLogin != null)
            throw new InvalidOperationException("У цього детектива вже є обліковий запис");

        var usernameExists = await context.Detectives.AnyAsync(d =>
            d.PostgresLogin != null && d.PostgresLogin.ToLower() == username.ToLower());
        if (usernameExists)
            throw new InvalidOperationException("Такий логін уже зайнятий");

        var emailExists = await context.Detectives.AnyAsync(d =>
            d.PostgresLogin != null &&
            d.Email.ToLower() == detective.Email.ToLower());
        if (emailExists)
            throw new InvalidOperationException("Обліковий запис для цього детектива вже існує");

        await context.Database.OpenConnectionAsync();
        try
        {
            var conn = (NpgsqlConnection)context.Database.GetDbConnection();
            try
            {
                await CreateDetectivePostgresRoleAsync(conn, username, password);
            }
            catch (PostgresException ex) when (ex.SqlState == "42710")
            {
                throw new InvalidOperationException("Такий логін уже зайнятий", ex);
            }
            catch (PostgresException ex) when (ex.SqlState == "42501")
            {
                throw new InvalidOperationException(
                    "Користувач підключення до БД не має права створювати ролі. Увійдіть як суперкористувач PostgreSQL і надайте ролі з рядка підключення атрибут CREATEROLE та право видавати членство в групі detective, наприклад: " +
                    "ALTER ROLE … CREATEROLE; GRANT detective TO … WITH ADMIN OPTION; (підставте ім’я ролі з Username у рядку підключення).",
                    ex);
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }

        try
        {
            detective.PostgresLogin = username;
            await context.SaveChangesAsync();
        }
        catch
        {
            await context.Database.OpenConnectionAsync();
            try
            {
                var conn = (NpgsqlConnection)context.Database.GetDbConnection();
                await DropDetectivePostgresRoleIfExistsAsync(conn, username);
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }

            throw;
        }

        return new DetectiveAccountCreatedDto
        {
            Username = username,
            Email = detective.Email
        };
    }

    private static string QuotePgIdent(string name) =>
        "\"" + name.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static string DollarQuoteForPg(string value)
    {
        for (var i = 0; ; i++)
        {
            var tag = i == 0 ? "cfpwd" : $"cfpwd{i}";
            var delim = "$" + tag + "$";
            if (!value.Contains(delim, StringComparison.Ordinal))
                return delim + value + delim;
        }
    }

    private static async Task CreateDetectivePostgresRoleAsync(NpgsqlConnection conn, string username, string password)
    {
        var pwdLiteral = DollarQuoteForPg(password);
        await using var create = new NpgsqlCommand(
            $"CREATE ROLE {QuotePgIdent(username)} WITH LOGIN PASSWORD {pwdLiteral} INHERIT", conn);
        await create.ExecuteNonQueryAsync();
        await using var grant = new NpgsqlCommand(
            $"GRANT detective TO {QuotePgIdent(username)}", conn);
        await grant.ExecuteNonQueryAsync();
    }

    private static async Task DropDetectivePostgresRoleIfExistsAsync(NpgsqlConnection conn, string roleName)
    {
        await using var cmd = new NpgsqlCommand(
            $"DROP ROLE IF EXISTS {QuotePgIdent(roleName)}", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private Task ReseedDetectiveIdSequenceAsync()
    {
        return context.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF pg_get_serial_sequence('detective', 'id') IS NOT NULL THEN
        PERFORM setval(
            pg_get_serial_sequence('detective', 'id'),
            COALESCE((SELECT MAX(id) FROM detective), 0),
            true
        );
    END IF;
END $$;
");
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

        if (!string.IsNullOrEmpty(detectiveEntity.PostgresLogin))
        {
            await context.Database.OpenConnectionAsync();
            try
            {
                var conn = (NpgsqlConnection)context.Database.GetDbConnection();
                await DropDetectivePostgresRoleIfExistsAsync(conn, detectiveEntity.PostgresLogin);
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }

        var cases = await context.Cases.Where(c => c.DetectiveId == detectiveId).ToListAsync();
        foreach (var c in cases)
            c.DetectiveId = null;

        context.Detectives.Remove(detectiveEntity);
        await context.SaveChangesAsync();

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

    #region Докази

    public async Task<Evidence?> GetEvidenceAsync(int evidenceId) =>
        await EvidencesVisibleToAdmin
            .Include(e => e.CreatedByDetective)
            .FirstOrDefaultAsync(e => e.Id == evidenceId);

    public async Task<List<Evidence>> GetEvidencesAsync() =>
        await EvidencesVisibleToAdmin.ToListAsync();

    public async Task<Evidence> CreateEvidenceAsync(CreateEvidenceDto dto)
    {
        var evidenceEntity = mapper.Map<Evidence>(dto);
        evidenceEntity.ApprovalStatus = ApprovalStatus.Approved;
        context.Evidences.Add(evidenceEntity);
        await context.SaveChangesAsync();
        return evidenceEntity;
    }

    public async Task<PagedResult<Evidence>> GetEvidencesPagedAsync(int pageNumber = 1, int pageSize = 15)
    {
        var query = EvidencesVisibleToAdmin;
        var allItems = await query.ToListAsync();

        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(e => e.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Evidence>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Evidence>> SearchEvidencesPagedAsync(string? searchTerm, int pageNumber = 1, int pageSize = 15)
    {
        var query = EvidencesVisibleToAdmin;
        var allItems = await query.ToListAsync();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            allItems = allItems.Where(e =>
                e.Id.ToString().Contains(term) ||
                e.Type.ToString().ToLower().Contains(term) ||
                e.Description.ToLower().Contains(term) ||
                e.CollectionDate.ToString().Contains(term) ||
                e.Region.ToLower().Contains(term) ||
                e.ApprovalStatus.ToString().ToLower().Contains(term))
                .ToList();
        }

        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(e => e.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Evidence>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<Evidence>> GetEvidencesFromCaseAsync(int caseId) =>
        await context.CaseEvidences
            .Where(ce => ce.CaseId == caseId)
            .Select(ce => ce.Evidence)
            .Where(e => e.ApprovalStatus != ApprovalStatus.Draft)
            .ToListAsync();

    public async Task<List<Case>> GetCasesByEvidenceIdAsync(int evidenceId)
    {
        if (!await EvidencesVisibleToAdmin.AnyAsync(e => e.Id == evidenceId))
            return [];

        return await context.Cases
            .Include(c => c.CaseType)
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .Where(c => context.CaseEvidences.Any(ce => ce.CaseId == c.Id && ce.EvidenceId == evidenceId))
            .ToListAsync();
    }

    public async Task<List<Evidence>> GetPendingEvidencesAsync() =>
        await EvidencesVisibleToAdmin
            .Where(e => e.ApprovalStatus == ApprovalStatus.Pending)
            .ToListAsync();

    public async Task<Evidence> ApproveEvidenceAsync(int evidenceId)
    {
        var evidence = await EvidencesVisibleToAdmin.FirstOrDefaultAsync(e => e.Id == evidenceId)
                     ?? throw new EntityNotFoundException("Evidence", evidenceId);

        evidence.ApprovalStatus = ApprovalStatus.Approved;
        await context.SaveChangesAsync();
        return evidence;
    }

    public async Task<Evidence> RejectEvidenceAsync(int evidenceId)
    {
        var evidence = await EvidencesVisibleToAdmin.FirstOrDefaultAsync(e => e.Id == evidenceId)
                     ?? throw new EntityNotFoundException("Evidence", evidenceId);

        evidence.ApprovalStatus = ApprovalStatus.Declined;
        await context.SaveChangesAsync();
        return evidence;
    }

    public async Task<Evidence> UpdateEvidenceAsync(int id, UpdateEvidenceDto dto)
    {
        var evidence = await EvidencesVisibleToAdmin.FirstOrDefaultAsync(e => e.Id == id)
                      ?? throw new EntityNotFoundException("Evidence", id);

        mapper.Map(dto, evidence);
        await context.SaveChangesAsync();

        return evidence;
    }

    public async Task<Evidence> SubmitEvidenceAsync(int evidenceId)
    {
        var evidence = await EvidencesVisibleToAdmin.FirstOrDefaultAsync(e => e.Id == evidenceId)
                     ?? throw new EntityNotFoundException("Evidence", evidenceId);

        evidence.ApprovalStatus = ApprovalStatus.Pending;
        await context.SaveChangesAsync();
        return evidence;
    }

    public async Task DeleteEvidenceAsync(int evidenceId)
    {
        var evidence = await EvidencesVisibleToAdmin.FirstOrDefaultAsync(e => e.Id == evidenceId)
                     ?? throw new EntityNotFoundException("Evidence", evidenceId);

        context.CaseEvidences.RemoveRange(
            await context.CaseEvidences.Where(ce => ce.EvidenceId == evidenceId).ToListAsync());
        context.Evidences.Remove(evidence);
        await context.SaveChangesAsync();
    }

    public async Task SetEvidenceStatusAsync(int evidenceId, ApprovalStatus status)
    {
        var evidence = await EvidencesVisibleToAdmin.FirstOrDefaultAsync(e => e.Id == evidenceId)
                     ?? throw new EntityNotFoundException("Evidence", evidenceId);

        evidence.ApprovalStatus = status;
        await context.SaveChangesAsync();
    }

    #endregion

    #region Підозрювані

    public async Task<Suspect?> GetSuspectAsync(int suspectId) =>
        await SuspectsVisibleToAdmin
            .Include(s => s.CreatedByDetective)
            .FirstOrDefaultAsync(s => s.Id == suspectId);

    public async Task<List<Suspect>> GetSuspectsAsync() =>
        await SuspectsVisibleToAdmin.ToListAsync();

    public async Task<PagedResult<Suspect>> GetSuspectsPagedAsync(int pageNumber = 1, int pageSize = 15)
    {
        var query = SuspectsVisibleToAdmin;
        var allItems = await query.ToListAsync();

        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(s => s.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Suspect>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Suspect>> SearchSuspectsPagedAsync(string? searchTerm, int pageNumber = 1, int pageSize = 15)
    {
        var query = SuspectsVisibleToAdmin;
        var allItems = await query.ToListAsync();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            allItems = allItems.Where(s =>
                s.Id.ToString().Contains(term) ||
                (s.FirstName != null && s.FirstName.ToLower().Contains(term)) ||
                (s.LastName != null && s.LastName.ToLower().Contains(term)) ||
                (s.FatherName != null && s.FatherName.ToLower().Contains(term)) ||
                (s.Nickname != null && s.Nickname.ToLower().Contains(term)) ||
                (s.PhoneNumber != null && s.PhoneNumber.Contains(term)) ||
                (s.City != null && s.City.ToLower().Contains(term)) ||
                (s.Region != null && s.Region.ToLower().Contains(term)) ||
                s.ApprovalStatus.ToString().ToLower().Contains(term))
                .ToList();
        }

        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(s => s.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Suspect>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<Suspect>> GetSuspectsFromCaseAsync(int caseId) =>
        await context.CaseSuspects
            .Where(cs => cs.CaseId == caseId)
            .Select(cs => cs.Suspect)
            .Where(s => s.ApprovalStatus != ApprovalStatus.Draft)
            .ToListAsync();

    public async Task<List<Case>> GetCasesBySuspectIdAsync(int suspectId)
    {
        if (!await SuspectsVisibleToAdmin.AnyAsync(s => s.Id == suspectId))
            return [];

        return await context.Cases
            .Include(c => c.CaseType)
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .Where(c => context.CaseSuspects.Any(cs => cs.CaseId == c.Id && cs.SuspectId == suspectId))
            .ToListAsync();
    }

    public async Task<List<Case>> GetCasesByCaseTypeIdAsync(int caseTypeId) =>
        await context.Cases
            .Include(c => c.CaseType)
            .Include(c => c.Client)
            .Include(c => c.Detective)
            .Where(c => c.CaseTypeId == caseTypeId)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();

    public async Task<List<Suspect>> GetPendingSuspectsAsync() =>
        await SuspectsVisibleToAdmin
            .Where(s => s.ApprovalStatus == ApprovalStatus.Pending)
            .ToListAsync();

    public async Task<Suspect> ApproveSuspectAsync(int suspectId)
    {
        var suspect = await SuspectsVisibleToAdmin.FirstOrDefaultAsync(s => s.Id == suspectId)
                     ?? throw new EntityNotFoundException("Suspect", suspectId);

        suspect.ApprovalStatus = ApprovalStatus.Approved;
        await context.SaveChangesAsync();
        return suspect;
    }

    public async Task<Suspect> RejectSuspectAsync(int suspectId)
    {
        var suspect = await SuspectsVisibleToAdmin.FirstOrDefaultAsync(s => s.Id == suspectId)
                     ?? throw new EntityNotFoundException("Suspect", suspectId);

        suspect.ApprovalStatus = ApprovalStatus.Declined;
        await context.SaveChangesAsync();
        return suspect;
    }

    public async Task<Suspect> UpdateSuspectAsync(int id, UpdateSuspectDto dto)
    {
        var suspect = await SuspectsVisibleToAdmin.FirstOrDefaultAsync(s => s.Id == id)
                     ?? throw new EntityNotFoundException("Suspect", id);

        mapper.Map(dto, suspect);
        await context.SaveChangesAsync();

        return suspect;
    }

    public async Task<Suspect> SubmitSuspectAsync(int suspectId)
    {
        var suspect = await SuspectsVisibleToAdmin.FirstOrDefaultAsync(s => s.Id == suspectId)
                     ?? throw new EntityNotFoundException("Suspect", suspectId);

        suspect.ApprovalStatus = ApprovalStatus.Pending;
        await context.SaveChangesAsync();
        return suspect;
    }

    public async Task DeleteSuspectAsync(int suspectId)
    {
        var suspect = await SuspectsVisibleToAdmin.FirstOrDefaultAsync(s => s.Id == suspectId)
                     ?? throw new EntityNotFoundException("Suspect", suspectId);

        context.CaseSuspects.RemoveRange(
            await context.CaseSuspects.Where(cs => cs.SuspectId == suspectId).ToListAsync());
        context.Suspects.Remove(suspect);
        await context.SaveChangesAsync();
    }

    public async Task SetSuspectStatusAsync(int suspectId, ApprovalStatus status)
    {
        var suspect = await SuspectsVisibleToAdmin.FirstOrDefaultAsync(s => s.Id == suspectId)
                     ?? throw new EntityNotFoundException("Suspect", suspectId);

        suspect.ApprovalStatus = status;
        await context.SaveChangesAsync();
    }

    #endregion

    #region Витрати

    public async Task<Expense?> GetExpenseAsync(int expenseId) =>
        await ExpensesVisibleToAdmin
            .Include(e => e.CreatedByDetective)
            .FirstOrDefaultAsync(e => e.Id == expenseId);

    public async Task<List<Expense>> GetExpensesAsync() =>
        await ExpensesVisibleToAdmin.ToListAsync();

    public async Task<PagedResult<Expense>> GetExpensesPagedAsync(int pageNumber = 1, int pageSize = 15)
    {
        var query = ExpensesVisibleToAdmin.Include(e => e.Case);
        var allItems = await query.ToListAsync();

        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(e => e.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Expense>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Expense>> SearchExpensesPagedAsync(string? searchTerm, int pageNumber = 1, int pageSize = 15)
    {
        var query = ExpensesVisibleToAdmin.Include(e => e.Case);
        var allItems = await query.ToListAsync();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            allItems = allItems.Where(e =>
                e.Id.ToString().Contains(term) ||
                (e.Case != null && e.Case.Title.ToLower().Contains(term)) ||
                e.Purpose.ToLower().Contains(term) ||
                e.Amount.ToString().Contains(term) ||
                e.DateTime.ToString().Contains(term) ||
                e.ApprovalStatus.ToString().ToLower().Contains(term))
                .ToList();
        }

        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(e => e.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Expense>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<Expense>> GetExpensesFromCaseAsync(int caseId) =>
        await ExpensesVisibleToAdmin.Where(e => e.CaseId == caseId).ToListAsync();

    public async Task<List<Expense>> GetPendingExpensesAsync() =>
        await ExpensesVisibleToAdmin.Where(e => e.ApprovalStatus == ApprovalStatus.Pending).ToListAsync();

    public async Task<Expense> ApproveExpenseAsync(int expenseId)
    {
        var expense = await ExpensesVisibleToAdmin.FirstOrDefaultAsync(e => e.Id == expenseId)
                     ?? throw new EntityNotFoundException("Expense", expenseId);
        
        expense.ApprovalStatus = ApprovalStatus.Approved;
        await context.SaveChangesAsync();
        
        return expense;
    }

    public async Task<Expense> RejectExpenseAsync(int expenseId)
    {
        var expense = await ExpensesVisibleToAdmin.FirstOrDefaultAsync(e => e.Id == expenseId)
                     ?? throw new EntityNotFoundException("Expense", expenseId);

        expense.ApprovalStatus = ApprovalStatus.Declined;
        await context.SaveChangesAsync();

        return expense;
    }

    public async Task<Expense> UpdateExpenseAsync(int id, UpdateExpenseDto dto)
    {
        var expense = await ExpensesVisibleToAdmin.FirstOrDefaultAsync(e => e.Id == id)
                     ?? throw new EntityNotFoundException("Expense", id);

        mapper.Map(dto, expense);
        await context.SaveChangesAsync();

        return expense;
    }

    public async Task<Expense> SubmitExpenseAsync(int expenseId)
    {
        var expense = await ExpensesVisibleToAdmin.FirstOrDefaultAsync(e => e.Id == expenseId)
                     ?? throw new EntityNotFoundException("Expense", expenseId);

        expense.ApprovalStatus = ApprovalStatus.Pending;
        await context.SaveChangesAsync();
        return expense;
    }

    public async Task DeleteExpenseAsync(int expenseId)
    {
        var expense = await ExpensesVisibleToAdmin.FirstOrDefaultAsync(e => e.Id == expenseId)
                     ?? throw new EntityNotFoundException("Expense", expenseId);

        context.Expenses.Remove(expense);
        await context.SaveChangesAsync();
    }

    public async Task SetExpenseStatusAsync(int expenseId, ApprovalStatus status)
    {
        var expense = await ExpensesVisibleToAdmin.FirstOrDefaultAsync(e => e.Id == expenseId)
                     ?? throw new EntityNotFoundException("Expense", expenseId);

        expense.ApprovalStatus = status;
        await context.SaveChangesAsync();
    }

    #endregion

    #region Звіти

    public async Task<Report?> GetReportAsync(int reportId) =>
        await ReportsVisibleToAdmin
            .Include(r => r.CreatedByDetective)
            .Include(r => r.Case)
            .FirstOrDefaultAsync(r => r.Id == reportId);

    public async Task<List<Report>> GetReportsAsync() =>
        await ReportsVisibleToAdmin.ToListAsync();

    public async Task<PagedResult<Report>> GetReportsPagedAsync(int pageNumber = 1, int pageSize = 15)
    {
        var query = ReportsVisibleToAdmin.Include(r => r.Case);
        var allItems = await query.ToListAsync();

        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(r => r.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Report>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Report>> SearchReportsPagedAsync(string? searchTerm, int pageNumber = 1, int pageSize = 15)
    {
        var query = ReportsVisibleToAdmin.Include(r => r.Case);
        var allItems = await query.ToListAsync();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            allItems = allItems.Where(r =>
                r.Id.ToString().Contains(term) ||
                (r.Case != null && r.Case.Title.ToLower().Contains(term)) ||
                r.Summary.ToLower().Contains(term) ||
                (r.Comments != null && r.Comments.ToLower().Contains(term)) ||
                r.ReportDate.ToString().Contains(term) ||
                r.ApprovalStatus.ToString().ToLower().Contains(term))
                .ToList();
        }

        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(r => r.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Report>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<Report>> GetReportsFromCaseAsync(int caseId) =>
        await ReportsVisibleToAdmin.Where(r => r.CaseId == caseId).ToListAsync();

    public async Task<List<Report>> GetPendingReportsAsync() =>
        await ReportsVisibleToAdmin.Where(r => r.ApprovalStatus == ApprovalStatus.Pending).ToListAsync();

    public async Task<Report> ApproveReportAsync(int reportId)
    {
        var report = await ReportsVisibleToAdmin.FirstOrDefaultAsync(r => r.Id == reportId)
                    ?? throw new EntityNotFoundException("Report", reportId);
        
        report.ApprovalStatus = ApprovalStatus.Approved;
        await context.SaveChangesAsync();
        
        return report;
    }

    public async Task<Report> RejectReportAsync(int reportId)
    {
        var report = await ReportsVisibleToAdmin.FirstOrDefaultAsync(r => r.Id == reportId)
                    ?? throw new EntityNotFoundException("Report", reportId);

        report.ApprovalStatus = ApprovalStatus.Declined;
        await context.SaveChangesAsync();

        return report;
    }

    public async Task<Report> UpdateReportAsync(int id, UpdateReportDto dto)
    {
        var report = await ReportsVisibleToAdmin.FirstOrDefaultAsync(r => r.Id == id)
                    ?? throw new EntityNotFoundException("Report", id);

        mapper.Map(dto, report);
        await context.SaveChangesAsync();

        return report;
    }

    public async Task<Report> SubmitReportAsync(int reportId)
    {
        var report = await ReportsVisibleToAdmin.FirstOrDefaultAsync(r => r.Id == reportId)
                    ?? throw new EntityNotFoundException("Report", reportId);

        report.ApprovalStatus = ApprovalStatus.Pending;
        await context.SaveChangesAsync();
        return report;
    }

    public async Task DeleteReportAsync(int reportId)
    {
        var report = await ReportsVisibleToAdmin.FirstOrDefaultAsync(r => r.Id == reportId)
                    ?? throw new EntityNotFoundException("Report", reportId);

        context.Reports.Remove(report);
        await context.SaveChangesAsync();
    }

    public async Task SetReportStatusAsync(int reportId, ApprovalStatus status)
    {
        var report = await ReportsVisibleToAdmin.FirstOrDefaultAsync(r => r.Id == reportId)
                    ?? throw new EntityNotFoundException("Report", reportId);

        report.ApprovalStatus = status;
        await context.SaveChangesAsync();
    }

    #endregion

    #region Типи справ

    public async Task<CaseType?> GetCaseTypeAsync(int caseTypeId) =>
        await context.CaseTypes.FindAsync(caseTypeId);

    public async Task<List<CaseType>> GetCaseTypesAsync() =>
        await context.CaseTypes.ToListAsync();

    public async Task<PagedResult<CaseType>> GetCaseTypesPagedAsync(int pageNumber = 1, int pageSize = 15)
    {
        var query = context.CaseTypes.AsQueryable();
        var allItems = await query.ToListAsync();

        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(ct => ct.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<CaseType>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<CaseType>> SearchCaseTypesPagedAsync(string? searchTerm, int pageNumber = 1, int pageSize = 15)
    {
        var query = context.CaseTypes.AsQueryable();
        var allItems = await query.ToListAsync();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            allItems = allItems.Where(ct =>
                ct.Id.ToString().Contains(term) ||
                ct.Name.ToLower().Contains(term) ||
                ct.Price.ToString().Contains(term))
                .ToList();
        }

        var totalCount = allItems.Count;
        var items = allItems
            .OrderBy(ct => ct.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

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
