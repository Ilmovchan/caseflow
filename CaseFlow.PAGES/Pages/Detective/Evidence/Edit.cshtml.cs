using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using CaseFlow.PAGES.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Evidence;

[Authorize(Policy = "DetectiveOnly")]
public class EditModel(DetectiveService detectiveService) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public CreateEvidenceInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Id = id;
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();

        var entity = await _detectiveService.GetEvidenceAsync(id, identity);
        if (entity == null)
            return NotFound();

        var status = entity.ApprovalStatus;
        if (status is not ApprovalStatus.Draft and not ApprovalStatus.Declined)
            return RedirectToPage("Details", new { id });

        var utc = entity.CollectionDate.Kind switch
        {
            DateTimeKind.Utc => entity.CollectionDate,
            DateTimeKind.Local => entity.CollectionDate.ToUniversalTime(),
            _ => DateTime.SpecifyKind(entity.CollectionDate, DateTimeKind.Utc)
        };
        var local = utc.ToLocalTime();

        Input = new CreateEvidenceInputModel
        {
            Type = entity.Type,
            Description = entity.Description,
            CollectionDate = new DateTime(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0, DateTimeKind.Local),
            Region = entity.Region,
            Annotation = entity.Annotation,
            Purpose = entity.Purpose
        };

        return Page();
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
            ModelState.AddModelError(nameof(Input.CollectionDate), "Дата збору не може бути в майбутньому");

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

            var dto = new UpdateEvidenceDto
            {
                Type = Input.Type,
                Description = Input.Description,
                CollectionDate = utcCollectionDate,
                Region = Input.Region,
                Annotation = Input.Annotation,
                Purpose = Input.Purpose
            };

            await _detectiveService.UpdateEvidenceAsync(Id, dto, identity);
            return RedirectToPage("Details", new { id = Id });
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            var constraintViolation = ConstraintViolationMapper.TryExtractConstraintViolation(ex);
            if (constraintViolation != null)
            {
                if (constraintViolation.ConstraintName == "collection_date_format")
                    ModelState.AddModelError(nameof(Input.CollectionDate), constraintViolation.UserFriendlyMessage);
                else
                    ModelState.AddModelError(string.Empty, constraintViolation.UserFriendlyMessage);
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Помилка при збереженні доказу. Спробуйте ще раз.");
            return Page();
        }
    }
}
