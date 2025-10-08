using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin;

[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    public string UserFullName { get; set; } = string.Empty;

    public void OnGet()
    {
        UserFullName = User.Identity?.Name ?? "Адміністратор";
    }
}
