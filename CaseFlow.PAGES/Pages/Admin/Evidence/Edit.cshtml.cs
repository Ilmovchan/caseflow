using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Evidence;

public class EditModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public UpdateEvidenceDto Input { get; set; } = new();

    public List<EvidenceType> EvidenceTypes { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetEvidenceAsync(id);
        if (entity == null) return NotFound();

        EvidenceTypes = Enum.GetValues(typeof(EvidenceType)).Cast<EvidenceType>().ToList();

        Id = entity.Id;
        Input = new UpdateEvidenceDto
        {
            Type = entity.Type,
            Description = entity.Description,
            CollectionDate = entity.CollectionDate,
            Region = entity.Region,
            Annotation = entity.Annotation,
            Purpose = entity.Purpose
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var updated = await _adminService.UpdateEvidenceAsync(Id, Input);
            // Set status to Draft after edit
            await _adminService.SetEvidenceStatusAsync(Id, ApprovalStatus.Draft);
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

            ModelState.AddModelError(string.Empty, "An error occurred while updating the evidence. Please try again.");
            return Page();
        }
    }
}

