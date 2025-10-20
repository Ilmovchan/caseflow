using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Reports;

[Authorize(Policy = "DetectiveOnly")]
public class EditModel(DetectiveService detectiveService) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public UpdateReportDto Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _detectiveService.GetReportAsync(id);
        if (entity == null) return NotFound();

        Id = entity.Id;
        Input = new UpdateReportDto
        {
            Summary = entity.Summary,
            Comments = entity.Comments
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var updated = await _detectiveService.UpdateReportAsync(Id, Input);
            return RedirectToPage("Details", new { id = updated.Id });
        }
        catch (EntityNotFoundException ex)
        {
            ModelState.AddModelError(string.Empty, $"Звіт не знайдений: {ex.Message}");
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

            ModelState.AddModelError(string.Empty, "Помилка при оновленні звіту. Спробуйте ще раз.");
            return Page();
        }
    }
}

