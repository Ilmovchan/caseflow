using AutoMapper;
using CaseFlow.BLL.Dto.CaseType;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.CaseTypes;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<CaseTypeDto> CaseTypes { get; set; } = new();
    public PagedResult<CaseTypeDto> PagedCaseTypes { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 15;

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;
        var pagedResult = await _adminService.GetCaseTypesPagedAsync(pageNumber, PageSize);
        PagedCaseTypes = new PagedResult<CaseTypeDto>
        {
            Items = _mapper.Map<List<CaseTypeDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };

        CaseTypes = PagedCaseTypes.Items;
    }

}
