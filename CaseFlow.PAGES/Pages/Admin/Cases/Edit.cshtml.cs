using CaseFlow.BLL.Dto.Case;
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

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enum
        .GetValues(typeof(CaseStatus))
        .Cast<CaseStatus>()
        .Select(s => new SelectListItem(s.ToString(), s.ToString()));

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetCaseAsync(id);
        if (entity == null) return NotFound();

        Id = entity.Id;
        Input = new UpdateCaseByAdminDto
        {
            Title = entity.Title,
            Description = entity.Description,
            ClientId = entity.ClientId,
            DetectiveId = entity.DetectiveId,
            CaseTypeId = entity.CaseTypeId,
            DeadlineDate = entity.DeadlineDate,
            CloseDate = entity.CloseDate,
            Status = entity.Status
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var updated = await _adminService.UpdateCaseAsync(Id, Input);
        return RedirectToPage("Details", new { id = updated.Id });
    }
}


