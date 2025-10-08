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
        
        // Add sample data if we have less than 10 case types
        if (CaseTypes.Count < 10)
        {
            CaseTypes.AddRange(GenerateSampleCaseTypes());
            // Re-sort the combined list to maintain ID order
            CaseTypes = CaseTypes.OrderBy(ct => ct.Id).ToList();
        }
    }
    
    private List<CaseTypeDto> GenerateSampleCaseTypes()
    {
        var sampleCaseTypes = new List<CaseTypeDto>();
        var random = new Random();
        
        var caseTypeNames = new[]
        {
            "Крадіжка", "Шахрайство", "Розшук особи", "Розслідування підпалу", "Вимагання",
            "Розшук злочинця", "Підробка документів", "Крадіжка зі зломом", "Розшук свідків",
            "Зловживання службовим становищем", "Шантаж", "Розшук доказів", "Розслідування підозри",
            "Обман клієнтів", "Розшук підозрюваного", "Розслідування інциденту", "Порушення закону",
            "Розшук інформації", "Розслідування події", "Розслідування злочину", "Розшук правди",
            "Розслідування факту", "Розслідування справи", "Розшук причини", "Розслідування обставин",
            "Розслідування випадку", "Розшук мотиву", "Розслідування деталей", "Розслідування епізоду",
            "Розшук зв'язків", "Розслідування зв'язків", "Розшук контактів", "Розслідування контактів",
            "Розшук адрес", "Розслідування адрес", "Розшук телефонів", "Розслідування телефонів",
            "Розшук електронної пошти", "Розслідування електронної пошти", "Розшук соціальних мереж",
            "Розслідування соціальних мереж", "Розшук фінансових операцій", "Розслідування фінансових операцій"
        };
        
        for (int i = 1; i <= 15; i++)
        {
            var basePrice = random.Next(5000, 50000); // 5000-50000 грн
            var price = basePrice + random.Next(0, 1000); // Add some variation
            
            sampleCaseTypes.Add(new CaseTypeDto
            {
                Id = CaseTypes.Count + i,
                Name = caseTypeNames[random.Next(caseTypeNames.Length)],
                Price = price
            });
        }
        
        return sampleCaseTypes;
    }
}
