using CaseFlow.BLL.Dto.Case;
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

    public async Task<IActionResult> OnPostAsync()
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

            var created = await _detectiveService.CreateSuspectAsync(Input.CaseId, dto, identity);
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

            ModelState.AddModelError(string.Empty, "Помилка при створенні підозрюваного. Спробуйте ще раз.");
            await OnGetAsync();
            return Page();
        }
    }
}

public class CreateSuspectInputModel
{
    public int CaseId { get; set; }
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


