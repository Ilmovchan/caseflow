using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Expenses;

[Authorize(Policy = "DetectiveOnly")]
public class CreateModel(DetectiveService detectiveService) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;

    [BindProperty]
    public CreateExpenseInputModel Input { get; set; } = new();

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
        if (!ModelState.IsValid)
            return Page();

        try
        {
            // Convert local datetime to UTC
            var utcDateTime = Input.DateTime.Kind == DateTimeKind.Local
                ? Input.DateTime.ToUniversalTime()
                : Input.DateTime;

            var dto = new CreateExpenseDto
            {
                DateTime = utcDateTime,
                Purpose = Input.Purpose,
                Amount = Input.Amount,
                Annotation = Input.Annotation,
                Status = ApprovalStatus.Draft
            };

            var created = await _detectiveService.CreateExpenseAsync(Input.CaseId, dto);
            return RedirectToPage("Details", new { id = created.Id });
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
                ModelState.AddModelError(string.Empty, constraintViolation.UserFriendlyMessage);
                await OnGetAsync();
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Помилка при створенні видатків. Спробуйте ще раз.");
            await OnGetAsync();
            return Page();
        }
    }
}

public class CreateExpenseInputModel
{
    public int CaseId { get; set; }
    public DateTime DateTime { get; set; } = DateTime.Now;
    public string Purpose { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? Annotation { get; set; }
}

