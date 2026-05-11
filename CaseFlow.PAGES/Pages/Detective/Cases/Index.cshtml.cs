using AutoMapper;
using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace CaseFlow.PAGES.Pages.Detective.Cases;

[Authorize(Policy = "DetectiveOnly")]
public class IndexModel(DetectiveService detectiveService, IMapper mapper) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;
    private readonly IMapper _mapper = mapper;

    public List<CaseDto> Cases { get; set; } = new();
    public PagedResult<CaseDto> PagedCases { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;

        var detectiveIdentity = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
        var allCases = string.IsNullOrWhiteSpace(detectiveIdentity)
            ? new List<Case>()
            : await _detectiveService.GetCasesByDetectiveEmailAsync(detectiveIdentity);

        var caseDtos = _mapper.Map<List<CaseDto>>(allCases);

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            caseDtos = caseDtos.Where(c =>
                c.Id.ToString().Contains(term) ||
                c.Title.ToLower().Contains(term) ||
                c.Status.ToString().ToLower().Contains(term) ||
                c.ClientFullName.ToLower().Contains(term) ||
                c.CaseTypeName.ToLower().Contains(term) ||
                c.StartDate.ToString().Contains(term) ||
                c.DeadlineDate.ToString().Contains(term) ||
                (c.CloseDate?.ToString() ?? "").Contains(term))
                .ToList();
        }

        var totalCount = caseDtos.Count;
        var items = caseDtos
            .OrderBy(c => c.Id)
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        PagedCases = new PagedResult<CaseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = PageSize
        };

        Cases = PagedCases.Items;
    }
}

