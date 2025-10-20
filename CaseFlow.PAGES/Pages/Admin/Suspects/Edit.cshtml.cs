using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Suspects;

public class EditModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public UpdateSuspectDto Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetSuspectAsync(id);
        if (entity == null) return NotFound();

        Id = entity.Id;
        Input = new UpdateSuspectDto
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
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var updated = await _adminService.UpdateSuspectAsync(Id, Input);
            return RedirectToPage("Details", new { id = updated.Id });
        }
        catch (Exception ex)
        {
            var constraintViolation = ConstraintViolationMapper.TryExtractConstraintViolation(ex);
            if (constraintViolation != null)
            {
                ModelState.AddModelError(string.Empty, constraintViolation.UserFriendlyMessage);
                return Page();
            }

            ModelState.AddModelError(string.Empty, "An error occurred while updating the suspect. Please try again.");
            return Page();
        }
    }
}

