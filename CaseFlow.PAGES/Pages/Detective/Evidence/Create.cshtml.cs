using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
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

    public List<CaseDto> Cases { get; set; } = new();

    public async Task OnGetAsync()
    {
        var cases = await _detectiveService.GetCasesAsync();
        Cases = cases.Select(c => new CaseDto
        {
            Id = c.Id,
            Title = c.Title,
            ClientFullName = c.Client?.FirstName + " " + c.Client?.LastName ?? "Unknown",
            CaseTypeName = c.CaseType?.Name ?? "Unknown"
        }).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Normalize to minute precision (drop seconds/milliseconds).
        var normalizedLocalDate = new DateTime(
                Input.CollectionDate.Year,
                Input.CollectionDate.Month,
                Input.CollectionDate.Day,
                Input.CollectionDate.Hour,
                Input.CollectionDate.Minute,
                0,
                DateTimeKind.Unspecified
            );

        // Validate against local time first (datetime-local is user local time, e.g. Kyiv).
        if (normalizedLocalDate > DateTime.Now)
        {
            ModelState.AddModelError(nameof(Input.CollectionDate), "Дата збору не може бути в майбутньому");
        }

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        try
        {
            // Convert local datetime to UTC for PostgreSQL.
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

            var created = await _detectiveService.CreateEvidenceAsync(Input.CaseId, dto);
            return RedirectToPage("Details", new { id = created.EvidenceId });
        }
        catch (EntityNotFoundException ex)
        {
            ModelState.AddModelError(string.Empty, $"Справа не знайдена: {ex.Message}");
            await OnGetAsync();
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
                await OnGetAsync();
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Помилка при створенні доказу. Спробуйте ще раз.");
            await OnGetAsync();
            return Page();
        }
    }
}

public class CreateEvidenceInputModel
{
    public int CaseId { get; set; }
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


