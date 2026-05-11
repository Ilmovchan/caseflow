using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using CaseFlow.PAGES.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CaseFlow.PAGES.Pages.Detective.Expenses;

[Authorize(Policy = "DetectiveOnly")]
[IgnoreAntiforgeryToken]
public class EditModel(DetectiveService detectiveService) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public UpdateExpenseInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        var entity = await _detectiveService.GetExpenseAsync(id, identity);
        if (entity == null) return NotFound();

        var status = entity.ApprovalStatus ?? ApprovalStatus.Draft;
        if (status is not ApprovalStatus.Draft and not ApprovalStatus.Declined)
            return RedirectToPage("Details", new { id });

        Id = entity.Id;
        Input = new UpdateExpenseInputModel
        {
            Purpose = entity.Purpose,
            Amount = entity.Amount,
            DateTime = entity.DateTime,
            Annotation = entity.Annotation
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var identity = DetectiveIdentity.FromUser(User);
            if (string.IsNullOrEmpty(identity))
                return Unauthorized();

            var utcDateTime = Input.DateTime.Kind == DateTimeKind.Local
                ? Input.DateTime.ToUniversalTime()
                : Input.DateTime;

            var dto = new UpdateExpenseDto
            {
                Purpose = Input.Purpose,
                Amount = Input.Amount,
                DateTime = utcDateTime,
                Annotation = Input.Annotation
            };

            var updated = await _detectiveService.UpdateExpenseAsync(Id, dto, identity);
            return RedirectToPage("Details", new { id = updated.Id });
        }
        catch (EntityNotFoundException ex)
        {
            ModelState.AddModelError(string.Empty, $"Видатки не знайдені: {ex.Message}");
            return Page();
        }
        catch (Exception ex)
        {
            var constraintViolation = ConstraintViolationMapper.TryExtractConstraintViolation(ex);
            if (constraintViolation != null)
            {
                ModelState.AddModelError(string.Empty, constraintViolation.UserFriendlyMessage);
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Помилка при оновленні видатків. Спробуйте ще раз.");
            return Page();
        }
    }
}

public class UpdateExpenseInputModel
{
    public string Purpose { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime DateTime { get; set; }
    public string? Annotation { get; set; }
}

