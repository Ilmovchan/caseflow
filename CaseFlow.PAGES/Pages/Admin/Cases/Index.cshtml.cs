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
        
        // Add sample data if we have less than 10 cases
        if (Cases.Count < 10)
        {
            Cases.AddRange(GenerateSampleCases());
            // Re-sort the combined list to maintain ID order
            Cases = Cases.OrderBy(c => c.Id).ToList();
        }
    }
    
    private List<CaseDto> GenerateSampleCases()
    {
        var sampleCases = new List<CaseDto>();
        var random = new Random();
        
        var caseTitles = new[]
        {
            "Розслідування крадіжки", "Слідство по шахрайству", "Розшук зниклої особи", 
            "Розслідування підпалу", "Слідство по вимаганню", "Розшук злочинця",
            "Розслідування підробки", "Слідство по крадіжці", "Розшук свідків",
            "Розслідування зловживання", "Слідство по шантажу", "Розшук доказів",
            "Розслідування підозри", "Слідство по обману", "Розшук підозрюваного",
            "Розслідування інциденту", "Слідство по порушенню", "Розшук інформації",
            "Розслідування події", "Слідство по злочину", "Розшук правди",
            "Розслідування факту", "Слідство по справі", "Розшук причини",
            "Розслідування обставин", "Слідство по випадку", "Розшук мотиву",
            "Розслідування деталей", "Слідство по епізоду", "Розшук зв'язків"
        };
        
        var statuses = new[] { CaseStatus.Opened, CaseStatus.Closed, CaseStatus.Paused };
        var clientNames = new[]
        {
            "Іван Петренко", "Марія Коваленко", "Олексій Сидоренко", "Анна Мельник",
            "Дмитро Бондаренко", "Тетяна Шевченко", "Сергій Кравченко", "Олена Морозенко",
            "Андрій Гриценко", "Наталія Левченко", "Володимир Козленко", "Ірина Савченко",
            "Михайло Романенко", "Світлана Ткаченко", "Олександр Гончаренко", "Людмила Федоренко"
        };
        
        var detectiveNames = new[]
        {
            "Петро Детективов", "Оксана Слідча", "Максим Розшуковець", "Юлія Слідча",
            "Артем Детектив", "Вікторія Розшук", "Денис Слідчий", "Катерина Детективка",
            "Роман Розшуковець", "Аліна Слідча", "Ігор Детектив", "Валентина Розшук",
            "Станіслав Слідчий", "Галина Детективка", "Віталій Розшуковець", "Лариса Слідча"
        };
        
        var caseTypes = new[] { "Крадіжка", "Шахрайство", "Розшук", "Слідство", "Розслідування" };
        
        for (int i = 1; i <= 25; i++)
        {
            var startDate = DateTime.Now.AddDays(-random.Next(1, 365));
            var deadlineDate = startDate.AddDays(random.Next(7, 90));
            
            sampleCases.Add(new CaseDto
            {
                Id = Cases.Count + i,
                Title = caseTitles[random.Next(caseTitles.Length)],
                Status = statuses[random.Next(statuses.Length)],
                ClientFullName = clientNames[random.Next(clientNames.Length)],
                DetectiveFullName = random.Next(2) == 0 ? detectiveNames[random.Next(detectiveNames.Length)] : null,
                CaseTypeName = caseTypes[random.Next(caseTypes.Length)],
                StartDate = DateOnly.FromDateTime(startDate),
                DeadlineDate = DateOnly.FromDateTime(deadlineDate),
                CloseDate = random.Next(3) == 0 ? DateOnly.FromDateTime(deadlineDate.AddDays(random.Next(-30, 30))) : null
            });
        }
        
        return sampleCases;
    }
}


