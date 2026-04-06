using CaseFlow.BLL.Dto.CaseType;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.CaseTypes;

public class EditModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public UpdateCaseTypeDto Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetCaseTypeAsync(id);
        if (entity == null) return NotFound();

        Id = entity.Id;
        Input = new UpdateCaseTypeDto
        {
            Name = entity.Name,
            Price = entity.Price
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var updated = await _adminService.UpdateCaseTypeAsync(Id, Input);
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

            ModelState.AddModelError(string.Empty, "An error occurred while updating the case type. Please try again.");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        try
        {
            await _adminService.DeleteCaseTypeAsync(Id);
            return RedirectToPage("Index");
        }
        catch (EntityDeleteConflictException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var entity = await _adminService.GetCaseTypeAsync(Id);
            if (entity == null) return RedirectToPage("Index");

            Id = entity.Id;
            Input = new UpdateCaseTypeDto
            {
                Name = entity.Name,
                Price = entity.Price
            };
            return Page();
        }
        catch (EntityNotFoundException)
        {
            return RedirectToPage("Index");
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "An error occurred while deleting the case type. Please try again.");
            return Page();
        }
    }
}
