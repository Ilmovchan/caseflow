using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CaseFlow.PAGES.Pages.Admin.DetectiveAccounts;

[Authorize(Policy = "AdminOnly")]
public class CreateModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> DetectiveOptions { get; set; } = new();

    public string? SuccessMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Оберіть детектива")]
        public int DetectiveId { get; set; }

        [Required(ErrorMessage = "Логін обов'язковий")]
        [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Логін: лише латиниця, цифри та _")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обов'язковий")]
        [MinLength(6, ErrorMessage = "Пароль має містити щонайменше 6 символів")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Підтвердіть пароль")]
        [Compare(nameof(Password), ErrorMessage = "Паролі не співпадають")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        await LoadDetectiveOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadDetectiveOptionsAsync();

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var created = await _adminService.CreateDetectiveAccountAsync(
                Input.DetectiveId,
                Input.Username,
                Input.Password
            );

            SuccessMessage = $"Акаунт створено успішно. Логін: {created.Username}, Email: {created.Email}";
            Input = new InputModel();
            await LoadDetectiveOptionsAsync();
            return Page();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    private async Task LoadDetectiveOptionsAsync()
    {
        var detectives = await _adminService.GetDetectivesWithoutAccountsAsync();
        DetectiveOptions = detectives
            .Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = $"#{d.Id} - {d.LastName} {d.FirstName} ({d.Email})"
            })
            .ToList();
    }
}
