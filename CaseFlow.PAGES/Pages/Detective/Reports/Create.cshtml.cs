using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using CaseFlow.PAGES.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Reports;

[Authorize(Policy = "DetectiveOnly")]
public class CreateModel(DetectiveService detectiveService) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;

    [BindProperty]
    public CreateReportInputModel Input { get; set; } = new();

    public List<CaseDto> Cases { get; set; } = new();

    public async Task OnGetAsync()
    {
        var identity = DetectiveIdentity.FromUser(User);
        var cases = string.IsNullOrEmpty(identity)
            ? []
            : await _detectiveService.GetCasesByDetectiveEmailAsync(identity);
        Cases = cases.Select(c => new CaseDto
        {
            Id = c.Id,
            Title = c.Title,
            ClientFullName = c.Client?.FirstName + " " + c.Client?.LastName ?? "Unknown",
            CaseTypeName = c.CaseType?.Name ?? "Unknown"
        }).ToList();
    }

    public Task<IActionResult> OnPostSubmitAsync() =>
        CreateReportInternalAsync(submitForApproval: true);

    public Task<IActionResult> OnPostDraftAsync() =>
        CreateReportInternalAsync(submitForApproval: false);

    private async Task<IActionResult> CreateReportInternalAsync(bool submitForApproval)
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        try
        {
            var identity = DetectiveIdentity.FromUser(User);
            if (string.IsNullOrEmpty(identity))
                return Unauthorized();

            var dto = new CreateReportDto
            {
                Summary = Input.Summary,
                Comments = Input.Comments
            };

            var created = await _detectiveService.CreateReportAsync(Input.CaseId, dto, identity, submitForApproval);
            return RedirectToPage("Details", new { id = created.Id });
        }
        catch (EntityNotFoundException ex)
        {
            ModelState.AddModelError(string.Empty, $"Справа не знайдена: {ex.Message}");
            await OnGetAsync();
            return Page();
        }
        catch (ArgumentException ex) when (ex.Message.Contains("DateTime with Kind=Local"))
        {
            ModelState.AddModelError(string.Empty, "Помилка з датою/часом. Спробуйте ще раз.");
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

            ModelState.AddModelError(string.Empty, "Помилка при створенні звіту. Спробуйте ще раз.");
            await OnGetAsync();
            return Page();
        }
    }
}

public class CreateReportInputModel
{
    public int CaseId { get; set; }
    public string Summary { get; set; } = null!;
    public string? Comments { get; set; }
}
