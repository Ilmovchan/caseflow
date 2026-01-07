using CaseFlow.BLL.Dto.Detective;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CaseFlow.PAGES.Pages.Admin.Detectives;

public class EditModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public UpdateDetectiveDto Input { get; set; } = new();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enum
        .GetValues(typeof(DetectiveStatus))
        .Cast<DetectiveStatus>()
        .Select(s => new SelectListItem(s.ToString(), s.ToString()));

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetDetectiveAsync(id);
        if (entity == null) return NotFound();

        Id = entity.Id;
        Input = new UpdateDetectiveDto
        {
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            FatherName = entity.FatherName,
            Email = entity.Email,
            PhoneNumber = entity.PhoneNumber,
            DateOfBirth = entity.DateOfBirth,
            Region = entity.Region,
            City = entity.City,
            Street = entity.Street,
            BuildingNumber = entity.BuildingNumber,
            ApartmentNumber = entity.ApartmentNumber,
            Salary = entity.Salary,
            PersonalNotes = entity.PersonalNotes,
            Status = entity.Status
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var updated = await _adminService.UpdateDetectiveAsync(Id, Input);
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

            ModelState.AddModelError(string.Empty, "An error occurred while updating the detective. Please try again.");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        try
        {
            await _adminService.DeleteDetectiveAsync(Id);
            return RedirectToPage("Index");
        }
        catch (CaseFlow.BLL.Exceptions.EntityDeleteConflictException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var entity = await _adminService.GetDetectiveAsync(Id);
            if (entity == null) return RedirectToPage("Index");
            
            Id = entity.Id;
            Input = new UpdateDetectiveDto
            {
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                FatherName = entity.FatherName,
                Email = entity.Email,
                PhoneNumber = entity.PhoneNumber,
                DateOfBirth = entity.DateOfBirth,
                Region = entity.Region,
                City = entity.City,
                Street = entity.Street,
                BuildingNumber = entity.BuildingNumber,
                ApartmentNumber = entity.ApartmentNumber,
                Salary = entity.Salary,
                PersonalNotes = entity.PersonalNotes,
                Status = entity.Status
            };
            return Page();
        }
        catch (CaseFlow.BLL.Exceptions.EntityNotFoundException)
        {
            return RedirectToPage("Index");
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "An error occurred while deleting the detective. Please try again.");
            return Page();
        }
    }
}


