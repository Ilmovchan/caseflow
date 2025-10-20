using CaseFlow.BLL.Dto.Detective;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Detectives;

public class CreateModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public CreateDetectiveDto Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var created = await _adminService.CreateDetectiveAsync(Input);
            return RedirectToPage("Details", new { id = created.Id });
        }
        catch (Exception ex)
        {
            var constraintViolation = ConstraintViolationMapper.TryExtractConstraintViolation(ex);
            if (constraintViolation != null)
            {
                ModelState.AddModelError(string.Empty, constraintViolation.UserFriendlyMessage);
                return Page();
            }

            ModelState.AddModelError(string.Empty, "An error occurred while creating the detective. Please try again.");
            return Page();
        }
    }
}


