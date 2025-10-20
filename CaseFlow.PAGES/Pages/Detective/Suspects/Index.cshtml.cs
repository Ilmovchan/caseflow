using AutoMapper;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Suspects;

[Authorize(Policy = "DetectiveOnly")]
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

        var allSuspects = await _detectiveService.GetSuspectsAsync();
        
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
}

