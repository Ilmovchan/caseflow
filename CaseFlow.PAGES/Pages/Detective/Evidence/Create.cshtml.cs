using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using CaseFlow.PAGES.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Evidence;

[Authorize(Policy = "DetectiveOnly")]
public class CreateModel(DetectiveService detectiveService) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;

    [BindProperty]
    public CreateEvidenceInputModel Input { get; set; } = new();

    public Task OnGetAsync() => Task.CompletedTask;

    public Task<IActionResult> OnPostSubmitAsync() =>
        CreateEvidenceInternalAsync(submitForApproval: true);

    public Task<IActionResult> OnPostDraftAsync() =>
        CreateEvidenceInternalAsync(submitForApproval: false);

    private async Task<IActionResult> CreateEvidenceInternalAsync(bool submitForApproval)
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
            var identity = DetectiveIdentity.FromUser(User);
            if (string.IsNullOrEmpty(identity))
                return Unauthorized();

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

            var created = await _detectiveService.CreateEvidenceAsync(dto, identity, submitForApproval);
            return RedirectToPage("Details", new { id = created.EvidenceId });
        }
        catch (EntityNotFoundException ex)
        {
            ModelState.AddModelError(string.Empty, $"Не вдалося створити доказ: {ex.Message}");
            return Page();
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

            ModelState.AddModelError(string.Empty, "Помилка при створенні доказу. Спробуйте ще раз.");
            return Page();
        }
    }
}

public class CreateEvidenceInputModel
{
    public CaseFlow.DAL.Enums.EvidenceType Type { get; set; }
    public string Description { get; set; } = null!;
    public DateTime CollectionDate { get; set; } = new DateTime(
        DateTime.Now.Year,
        DateTime.Now.Month,
        DateTime.Now.Day,
        DateTime.Now.Hour,
        DateTime.Now.Minute,
        0
    );
    public string Region { get; set; } = "Не вказано";
    public string? Annotation { get; set; }
    public string? Purpose { get; set; }
}
