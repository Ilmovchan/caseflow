using CaseFlow.BLL.Dto.Client;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Clients;

public class EditModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public UpdateClientDto Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetClientAsync(id);
        if (entity == null) return NotFound();

        Id = entity.Id;
        Input = new UpdateClientDto
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
            ApartmentNumber = entity.ApartmentNumber
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var updated = await _adminService.UpdateClientAsync(Id, Input);
            return RedirectToPage("Details", new { id = updated.Id });
        }
        catch (Exception ex)
        {
            var constraintViolation = CaseFlow.BLL.Exceptions.ConstraintViolationMapper.TryExtractConstraintViolation(ex);
            if (constraintViolation != null)
            {
                ModelState.AddModelError(string.Empty, constraintViolation.UserFriendlyMessage);
                return Page();
            }

            ModelState.AddModelError(string.Empty, "An error occurred while updating the client. Please try again.");
            return Page();
        }
    }
}


