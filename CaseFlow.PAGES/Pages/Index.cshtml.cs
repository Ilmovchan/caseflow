using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
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
        
        return RedirectToPage("/Auth/Login");
    }
}