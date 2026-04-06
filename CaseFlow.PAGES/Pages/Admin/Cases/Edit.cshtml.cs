using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CaseFlow.PAGES.Pages.Admin.Cases;

public class EditModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public UpdateCaseByAdminDto Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enum
        .GetValues(typeof(CaseStatus))
        .Cast<CaseStatus>()
        .Select(s => new SelectListItem(s.ToString(), s.ToString()));

    public SelectList ClientOptions { get; set; } = null!;
    public SelectList DetectiveOptions { get; set; } = null!;
    public SelectList CaseTypeOptions { get; set; } = null!;
    
    public CaseFlow.DAL.Models.Case? Case { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetCaseAsync(id);
        if (entity == null) return NotFound();

        Case = entity;
        Id = entity.Id;
        Input = new UpdateCaseByAdminDto
        {
            Title = entity.Title,
            Description = entity.Description,
            ClientId = entity.ClientId,
            DetectiveId = entity.DetectiveId,
            CaseTypeId = entity.CaseTypeId,
            DeadlineDate = entity.DeadlineDate,
            Status = entity.Status
        };

        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Case = await _adminService.GetCaseAsync(Id);
            await LoadOptionsAsync();
            return Page();
        }

        var updated = await _adminService.UpdateCaseAsync(Id, Input);
        return RedirectToPage("Details", new { id = updated.Id });
    }

    public async Task<IActionResult> OnPostCloseCaseAsync()
    {
        var caseEntity = await _adminService.GetCaseAsync(Id);
        if (caseEntity == null) return NotFound();

        await _adminService.UpdateCaseAsync(Id, new UpdateCaseByAdminDto
        {
            Title = caseEntity.Title,
            Description = caseEntity.Description,
            ClientId = caseEntity.ClientId,
            DetectiveId = caseEntity.DetectiveId,
            CaseTypeId = caseEntity.CaseTypeId,
            DeadlineDate = caseEntity.DeadlineDate,
            CloseDate = DateOnly.FromDateTime(DateTime.Now),
            Status = CaseStatus.Closed
        });

        return RedirectToPage("Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        try
        {
            await _adminService.DeleteCaseAsync(Id);
            return RedirectToPage("Index");
        }
        catch (EntityDeleteConflictException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (EntityNotFoundException)
        {
            return RedirectToPage("Index");
        }

        var entity = await _adminService.GetCaseAsync(Id);
        if (entity == null) return RedirectToPage("Index");

        Case = entity;
        Id = entity.Id;
        Input = new UpdateCaseByAdminDto
        {
            Title = entity.Title,
            Description = entity.Description,
            ClientId = entity.ClientId,
            DetectiveId = entity.DetectiveId,
            CaseTypeId = entity.CaseTypeId,
            DeadlineDate = entity.DeadlineDate,
            Status = entity.Status
        };

        await LoadOptionsAsync();
        return Page();
    }

    private async Task LoadOptionsAsync()
    {
        var clients = await _adminService.GetClientsAsync();
        ClientOptions = new SelectList(
            clients.Select(c => new { 
                Id = c.Id, 
                Name = $"{c.LastName} {c.FirstName}" + (c.FatherName != null ? $" {c.FatherName}" : "") 
            }),
            "Id",
            "Name",
            Input.ClientId
        );

        var detectives = await _adminService.GetDetectivesAsync();
        DetectiveOptions = new SelectList(
            detectives.Select(d => new { 
                Id = d.Id, 
                Name = $"{d.LastName} {d.FirstName}" + (d.FatherName != null ? $" {d.FatherName}" : "") 
            }),
            "Id",
            "Name",
            Input.DetectiveId
        );

        var caseTypes = await _adminService.GetCaseTypesAsync();
        CaseTypeOptions = new SelectList(
            caseTypes.Select(ct => new { Id = ct.Id, Name = ct.Name }),
            "Id",
            "Name",
            Input.CaseTypeId
        );
    }
}


