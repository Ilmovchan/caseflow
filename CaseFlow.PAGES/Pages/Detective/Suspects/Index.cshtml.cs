using AutoMapper;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Services;
using CaseFlow.PAGES.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CaseFlow.PAGES.Pages.Detective.Suspects;

[Authorize(Policy = "DetectiveOnly")]
[IgnoreAntiforgeryToken]
public class IndexModel(DetectiveService detectiveService, IMapper mapper) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;
    private readonly IMapper _mapper = mapper;

    public List<SuspectDto> Suspects { get; set; } = new();
    public PagedResult<SuspectDto> PagedSuspects { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;

        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
        {
            Suspects = [];
            return;
        }

        var allSuspects = await _detectiveService.GetSuspectsAsync(identity);
        
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            allSuspects = allSuspects.Where(s =>
                s.Id.ToString().Contains(term) ||
                (s.FirstName?.ToLower().Contains(term) ?? false) ||
                (s.LastName?.ToLower().Contains(term) ?? false) ||
                (s.FatherName?.ToLower().Contains(term) ?? false) ||
                (s.Nickname?.ToLower().Contains(term) ?? false) ||
                (s.PhoneNumber?.Contains(term) ?? false) ||
                (s.City?.ToLower().Contains(term) ?? false))
                .ToList();
        }

        var totalCount = allSuspects.Count;
        var items = allSuspects
            .OrderBy(s => s.Id)
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        PagedSuspects = new PagedResult<SuspectDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = PageSize
        };

        Suspects = PagedSuspects.Items;
    }

    public async Task<IActionResult> OnPostApproveAsync([FromQuery] int id)
    {
        try
        {
            var identity = DetectiveIdentity.FromUser(User);
            if (string.IsNullOrEmpty(identity))
                return Unauthorized();
            var submitted = await _detectiveService.SubmitSuspectAsync(id, identity);
            return new JsonResult(new {
                success = true,
                newStatus = "Pending",
                newStatusText = "Очікує",
                newStatusColor = "warning",
                approvalStatus = "Pending",
                message = "Підозрюваний надіслано на перевірку!",
                suspect = submitted
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
            await _detectiveService.DeleteSuspectAsync(id, identity);
            return new JsonResult(new {
                success = true,
                message = "Підозрюваний видалено успішно"
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
