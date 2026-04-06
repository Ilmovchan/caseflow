using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace CaseFlow.PAGES.Pages.Detective.Account;

[Authorize(Policy = "DetectiveOnly")]
public class IndexModel(DetectiveService detectiveService) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;

    public CaseFlow.DAL.Models.Detective? Detective { get; set; }
    public string? UserEmail { get; set; }
    public string? Username { get; set; }

    public async Task OnGetAsync()
    {
        UserEmail = User.FindFirstValue(ClaimTypes.Email);
        Username = User.Identity?.Name;

        if (!string.IsNullOrWhiteSpace(UserEmail))
        {
            Detective = await _detectiveService.GetDetectiveByEmailAsync(UserEmail);
        }
    }
}
