using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using CaseFlow.PAGES.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Suspects;

[Authorize(Policy = "DetectiveOnly")]
public class CreateModel(DetectiveService detectiveService) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;

    [BindProperty]
    public CreateSuspectInputModel Input { get; set; } = new();

    public Task OnGetAsync() => Task.CompletedTask;

    public Task<IActionResult> OnPostSubmitAsync() =>
        CreateSuspectInternalAsync(submitForApproval: true);

    public Task<IActionResult> OnPostDraftAsync() =>
        CreateSuspectInternalAsync(submitForApproval: false);

    private async Task<IActionResult> CreateSuspectInternalAsync(bool submitForApproval)
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var identity = DetectiveIdentity.FromUser(User);
            if (string.IsNullOrEmpty(identity))
                return Unauthorized();

            var dto = new CreateSuspectDto
            {
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                FatherName = Input.FatherName,
                Nickname = Input.Nickname,
                PhoneNumber = Input.PhoneNumber,
                DateOfBirth = Input.DateOfBirth,
                Region = Input.Region,
                City = Input.City,
                Street = Input.Street,
                BuildingNumber = Input.BuildingNumber,
                ApartmentNumber = Input.ApartmentNumber,
                Height = Input.Height,
                Weight = Input.Weight,
                PhysicalDescription = Input.PhysicalDescription,
                PriorConvictions = Input.PriorConvictions
            };

            var created = await _detectiveService.CreateSuspectAsync(dto, identity, submitForApproval);
            return RedirectToPage("Details", new { id = created.Id });
        }
        catch (SuspectValidationException ex)
        {
            foreach (var (prop, msg) in ex.Errors)
                ModelState.AddModelError($"Input.{prop}", msg);
            return Page();
        }
        catch (EntityNotFoundException ex)
        {
            ModelState.AddModelError(string.Empty, $"Не вдалося створити підозрюваного: {ex.Message}");
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

            ModelState.AddModelError(string.Empty, "Помилка при створенні підозрюваного. Спробуйте ще раз.");
            return Page();
        }
    }
}

public class CreateSuspectInputModel
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FatherName { get; set; }
    public string? Nickname { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? BuildingNumber { get; set; }
    public int? ApartmentNumber { get; set; }
    public int? Height { get; set; }
    public int? Weight { get; set; }
    public string? PhysicalDescription { get; set; }
    public string? PriorConvictions { get; set; }
}
