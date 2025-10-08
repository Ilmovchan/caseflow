using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages;

public class IndexModel : PageModel
{
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
        
        // Redirect unauthenticated users to login
        return RedirectToPage("/Auth/Login");
    }
}