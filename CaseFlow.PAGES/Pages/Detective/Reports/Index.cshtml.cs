using AutoMapper;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Services;
using CaseFlow.PAGES.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CaseFlow.PAGES.Pages.Detective.Reports;

[Authorize(Policy = "DetectiveOnly")]
[IgnoreAntiforgeryToken]
public class IndexModel(DetectiveService detectiveService, IMapper mapper) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;
    private readonly IMapper _mapper = mapper;

    public List<ReportDto> Reports { get; set; } = new();
    public PagedResult<ReportDto> PagedReports { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;

        var identity = DetectiveIdentity.FromUser(User);
        var allReports = string.IsNullOrEmpty(identity)
            ? []
            : await _detectiveService.GetReportsAsync(identity);
        
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            allReports = allReports.Where(r =>
                r.Id.ToString().Contains(term) ||
                r.CaseId.ToString().Contains(term) ||
                r.ReportDate.ToString().Contains(term) ||
                (r.ApprovalStatus?.ToString().ToLower() ?? "").Contains(term) ||
                (!string.IsNullOrEmpty(r.Summary) && r.Summary.ToLower().Contains(term)))
                .ToList();
        }

        var totalCount = allReports.Count;
        var items = allReports
            .OrderBy(r => r.Id)
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        PagedReports = new PagedResult<ReportDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = PageSize
        };

        Reports = PagedReports.Items;
    }

    public async Task<IActionResult> OnPostApproveAsync([FromQuery] int id)
    {
        try
        {
            var identity = DetectiveIdentity.FromUser(User);
            if (string.IsNullOrEmpty(identity))
                return Unauthorized();
            var submitted = await _detectiveService.SubmitReportAsync(id, identity);
            return new JsonResult(new {
                success = true,
                newStatus = "Pending",
                newStatusText = "Очікує",
                newStatusColor = "warning",
                approvalStatus = "Pending",
                message = "Звіт надіслано на перевірку!",
                report = submitted
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new {
                success = false,
                error = ex.Message
            });
        }
    }

    public async Task<IActionResult> OnPostRejectAsync([FromQuery] int id)
    {
        try
        {
            var identity = DetectiveIdentity.FromUser(User);
            if (string.IsNullOrEmpty(identity))
                return Unauthorized();
            await _detectiveService.DeleteReportAsync(id, identity);
            return new JsonResult(new {
                success = true,
                message = "Звіт видалено успішно"
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new {
                success = false,
                error = ex.Message
            });
        }
    }
}

