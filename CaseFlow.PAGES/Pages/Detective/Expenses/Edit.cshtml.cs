using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Expenses;

[Authorize(Policy = "DetectiveOnly")]
public class EditModel(DetectiveService detectiveService) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public UpdateExpenseInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _detectiveService.GetExpenseAsync(id);
        if (entity == null) return NotFound();

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
            // Convert local datetime to UTC
            var utcDateTime = Input.DateTime.Kind == DateTimeKind.Local
                ? Input.DateTime.ToUniversalTime()
                : Input.DateTime;

            var dto = new UpdateExpenseDto
            {
                Purpose = Input.Purpose,
                Amount = Input.Amount,
                DateTime = utcDateTime,
                Annotation = Input.Annotation,
                Status = ApprovalStatus.Draft
            };

            var updated = await _detectiveService.UpdateExpenseAsync(Id, dto);
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

