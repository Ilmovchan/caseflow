using CaseFlow.BLL.Dto.Auth;
using CaseFlow.BLL.Services;
using CaseFlow.PAGES;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CaseFlow.PAGES.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly AuthService _authService;

    public LoginModel(AuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    [Required(ErrorMessage = "Ім'я користувача обов'язкове")]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Пароль обов'язковий")]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public bool RememberMe { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        // If user is already authenticated, redirect to appropriate dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToPage("/Admin/Index");
            }
            else if (User.IsInRole("Detective"))
            {
                return RedirectToPage("/Detective/Index");
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var loginDto = new LoginDto
            {
                Username = Username,
                Password = Password,
                RememberMe = RememberMe
            };

            var result = await _authService.LoginAsync(loginDto);

            if (result.Success && result.User != null)
            {
                await HttpContext.Session.LoadAsync();
                HttpContext.Session.SetString(PgSessionKeys.Password, Password);

                // Create claims
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, result.User.Username),
                    new(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
                    new(ClaimTypes.Email, result.User.Email ?? string.Empty),
                    new(ClaimTypes.Role, result.User.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = RememberMe,
                    ExpiresUtc = RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                // Redirect to appropriate dashboard
                if (result.User.Role == "Admin")
                {
                    return RedirectToPage("/Admin/Index");
                }
                else if (result.User.Role == "Detective")
                {
                    return RedirectToPage("/Detective/Index");
                }
            }
            else
            {
                ErrorMessage = result.Message ?? "Невідома помилка входу";
                return Page();
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Сталася помилка під час входу. Спробуйте ще раз.";
            return Page();
        }

        return Page();
    }
}
