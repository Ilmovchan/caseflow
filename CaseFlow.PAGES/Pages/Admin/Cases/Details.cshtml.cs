using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Cases;

public class DetailsModel(AdminService adminService) : PageModel
{
    private readonly AdminService _adminService = adminService;

    public Case Case { get; set; } = null!;
    public List<CaseFlow.DAL.Models.Evidence> Evidences { get; set; } = new();
    public List<Suspect> Suspects { get; set; } = new();
    public List<Report> Reports { get; set; } = new();
    public List<Expense> Expenses { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entity = await _adminService.GetCaseAsync(id);
        if (entity == null) return NotFound();
        Case = entity;

        Evidences = (await _adminService.GetEvidencesFromCaseAsync(id))
            .Where(e => e.ApprovalStatus != ApprovalStatus.Draft)
            .ToList();
        Suspects = (await _adminService.GetSuspectsFromCaseAsync(id))
            .Where(s => s.ApprovalStatus != ApprovalStatus.Draft)
            .ToList();
        Reports = (await _adminService.GetReportsFromCaseAsync(id))
            .Where(r => r.ApprovalStatus != ApprovalStatus.Draft)
            .ToList();
        Expenses = (await _adminService.GetExpensesFromCaseAsync(id))
            .Where(e => e.ApprovalStatus != ApprovalStatus.Draft)
            .ToList();

        return Page();
    }
}


