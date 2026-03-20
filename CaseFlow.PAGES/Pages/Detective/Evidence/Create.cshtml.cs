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
        // Validate collection date is not in the future
        if (Input.CollectionDate > DateTime.UtcNow)
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
            // Convert local datetime to UTC for PostgreSQL
            var utcCollectionDate = Input.CollectionDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(Input.CollectionDate, DateTimeKind.Local).ToUniversalTime()
                : Input.CollectionDate.Kind == DateTimeKind.Local
                    ? Input.CollectionDate.ToUniversalTime()
                    : Input.CollectionDate;

            // Double-check the date is not in the future after conversion
            if (utcCollectionDate > DateTime.UtcNow)
            {
                ModelState.AddModelError(nameof(Input.CollectionDate), "Дата збору не може бути в майбутньому");
                await OnGetAsync();
                return Page();
            }

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
    public DateTime CollectionDate { get; set; } = DateTime.Now;
    public string Region { get; set; } = "Не вказано";
    public string? Annotation { get; set; }
    public string? Purpose { get; set; }
}


