using AutoMapper;
using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Cases;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<CaseDto> Cases { get; set; } = new();
    public PagedResult<CaseDto> PagedCases { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 15;

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;
        var pagedResult = await _adminService.GetCasesPagedAsync(pageNumber, PageSize);
        PagedCases = new PagedResult<CaseDto>
        {
            Items = _mapper.Map<List<CaseDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };

        Cases = PagedCases.Items;
    }

}


