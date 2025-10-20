using AutoMapper;
using CaseFlow.BLL.Dto.Detective;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Detectives;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<DetectiveDto> Detectives { get; set; } = new();
    public PagedResult<DetectiveDto> PagedDetectives { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;

        PagedResult<CaseFlow.DAL.Models.Detective> pagedResult;
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            pagedResult = await _adminService.SearchDetectivesPagedAsync(SearchTerm, pageNumber, PageSize);
        }
        else
        {
            pagedResult = await _adminService.GetDetectivesPagedAsync(pageNumber, PageSize);
        }

        PagedDetectives = new PagedResult<DetectiveDto>
        {
            Items = _mapper.Map<List<DetectiveDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };

        Detectives = PagedDetectives.Items;
    }

}


