using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Evidence;

public class CreateModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public CreateEvidenceDto Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var normalizedLocalDate = new DateTime(
                Input.CollectionDate.Year,
                Input.CollectionDate.Month,
                Input.CollectionDate.Day,
                Input.CollectionDate.Hour,
                Input.CollectionDate.Minute,
                0,
                DateTimeKind.Unspecified
            );

        if (normalizedLocalDate > DateTime.Now)
        {
            ModelState.AddModelError(nameof(Input.CollectionDate), "Дата збору не може бути в майбутньому");
        }

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var utcCollectionDate = normalizedLocalDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(normalizedLocalDate, DateTimeKind.Local).ToUniversalTime()
                : normalizedLocalDate.Kind == DateTimeKind.Local
                    ? normalizedLocalDate.ToUniversalTime()
                    : normalizedLocalDate;

            var dto = new CreateEvidenceDto
            {
                Type = Input.Type,
                Description = Input.Description,
                CollectionDate = utcCollectionDate,
                Region = Input.Region,
                Annotation = Input.Annotation,
                Purpose = Input.Purpose
            };

            var created = await _adminService.CreateEvidenceAsync(dto);
            return RedirectToPage("Details", new { id = created.Id });
        }
        catch (Exception ex)
        {
            var constraintViolation = ConstraintViolationMapper.TryExtractConstraintViolation(ex);
            if (constraintViolation != null)
            {
                if (constraintViolation.ConstraintName == "collection_date_format")
                {
                    ModelState.AddModelError(nameof(Input.CollectionDate), constraintViolation.UserFriendlyMessage);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, constraintViolation.UserFriendlyMessage);
                }
                return Page();
            }

            ModelState.AddModelError(string.Empty, "An error occurred while creating the evidence. Please try again.");
            return Page();
        }
    }
}

