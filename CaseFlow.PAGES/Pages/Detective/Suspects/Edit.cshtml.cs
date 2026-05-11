using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using CaseFlow.BLL.Validation;
using CaseFlow.DAL.Enums;
using CaseFlow.PAGES.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Suspects;

[Authorize(Policy = "DetectiveOnly")]
public class EditModel(DetectiveService detectiveService) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public CreateSuspectInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Id = id;
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();

        var entity = await _detectiveService.GetSuspectAsync(id, identity);
        if (entity == null)
            return NotFound();

        var status = entity.ApprovalStatus;
        if (status is not ApprovalStatus.Draft and not ApprovalStatus.Declined)
            return RedirectToPage("Details", new { id });

        Input = new CreateSuspectInputModel
        {
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            FatherName = entity.FatherName,
            Nickname = entity.Nickname,
            PhoneNumber = entity.PhoneNumber,
            DateOfBirth = entity.DateOfBirth,
            Region = entity.Region,
            City = entity.City,
            Street = entity.Street,
            BuildingNumber = entity.BuildingNumber,
            ApartmentNumber = entity.ApartmentNumber,
            Height = entity.Height,
            Weight = entity.Weight,
            PhysicalDescription = entity.PhysicalDescription,
            PriorConvictions = entity.PriorConvictions
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var validateDto = new CreateSuspectDto
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
        SuspectDatabaseRules.TrimNullableStrings(validateDto);
        foreach (var (prop, msg) in SuspectDatabaseRules.GetCreateViolations(validateDto))
            ModelState.AddModelError($"Input.{prop}", msg);

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var identity = DetectiveIdentity.FromUser(User);
            if (string.IsNullOrEmpty(identity))
                return Unauthorized();

            var dto = new UpdateSuspectDto
            {
                FirstName = validateDto.FirstName,
                LastName = validateDto.LastName,
                FatherName = validateDto.FatherName,
                Nickname = validateDto.Nickname,
                PhoneNumber = validateDto.PhoneNumber,
                DateOfBirth = validateDto.DateOfBirth,
                Region = validateDto.Region,
                City = validateDto.City,
                Street = validateDto.Street,
                BuildingNumber = validateDto.BuildingNumber,
                ApartmentNumber = validateDto.ApartmentNumber,
                Height = validateDto.Height,
                Weight = validateDto.Weight,
                PhysicalDescription = validateDto.PhysicalDescription,
                PriorConvictions = validateDto.PriorConvictions
            };

            await _detectiveService.UpdateSuspectAsync(Id, dto, identity);
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
                ModelState.AddModelError(string.Empty, constraintViolation.UserFriendlyMessage);
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Помилка при збереженні підозрюваного. Спробуйте ще раз.");
            return Page();
        }
    }
}
