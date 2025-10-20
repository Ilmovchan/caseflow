using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Reports;

public class EditModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public UpdateReportDto Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetReportAsync(id);
        if (entity == null) return NotFound();

        Id = entity.Id;
        Input = new UpdateReportDto
        {
            Summary = entity.Summary,
            Comments = entity.Comments
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var updated = await _adminService.UpdateReportAsync(Id, Input);
            return RedirectToPage("Details", new { id = updated.Id });
        }
        catch (Exception ex)
        {
            var constraintViolation = ConstraintViolationMapper.TryExtractConstraintViolation(ex);
            if (constraintViolation != null)
            {
                ModelState.AddModelError(string.Empty, constraintViolation.UserFriendlyMessage);
                return Page();
            }

            ModelState.AddModelError(string.Empty, "An error occurred while updating the report. Please try again.");
            return Page();
        }
    }
}

