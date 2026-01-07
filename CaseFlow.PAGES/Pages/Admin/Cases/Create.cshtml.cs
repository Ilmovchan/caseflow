using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CaseFlow.PAGES.Pages.Admin.Cases;

public class CreateModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    [BindProperty]
    public CreateCaseDto Input { get; set; } = new();

    public SelectList ClientOptions { get; set; } = null!;
    public SelectList DetectiveOptions { get; set; } = null!;
    public SelectList CaseTypeOptions { get; set; } = null!;

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        var created = await _adminService.CreateCaseAsync(Input);
        return RedirectToPage("Details", new { id = created.Id });
    }

    private async Task LoadOptionsAsync()
    {
        var clients = await _adminService.GetClientsAsync();
        ClientOptions = new SelectList(
            clients.Select(c => new { 
                Id = c.Id, 
                Name = $"{c.LastName} {c.FirstName}" + (c.FatherName != null ? $" {c.FatherName}" : "") 
            }),
            "Id",
            "Name"
        );

        var detectives = await _adminService.GetDetectivesAsync();
        DetectiveOptions = new SelectList(
            detectives.Select(d => new { 
                Id = d.Id, 
                Name = $"{d.LastName} {d.FirstName}" + (d.FatherName != null ? $" {d.FatherName}" : "") 
            }),
            "Id",
            "Name"
        );

        var caseTypes = await _adminService.GetCaseTypesAsync();
        CaseTypeOptions = new SelectList(
            caseTypes.Select(ct => new { Id = ct.Id, Name = ct.Name }),
            "Id",
            "Name"
        );
    }
}


