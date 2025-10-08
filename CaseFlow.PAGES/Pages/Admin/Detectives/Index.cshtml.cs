using AutoMapper;
using CaseFlow.BLL.Dto.Detective;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Detectives;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<DetectiveDto> Detectives { get; set; } = new();
    public PagedResult<DetectiveDto> PagedDetectives { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 15;

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;
        var pagedResult = await _adminService.GetDetectivesPagedAsync(pageNumber, PageSize);
        PagedDetectives = new PagedResult<DetectiveDto>
        {
            Items = _mapper.Map<List<DetectiveDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };
        
        Detectives = PagedDetectives.Items;
        
        // Add sample data if we have less than 10 detectives
        if (Detectives.Count < 10)
        {
            Detectives.AddRange(GenerateSampleDetectives());
            // Re-sort the combined list to maintain ID order
            Detectives = Detectives.OrderBy(d => d.Id).ToList();
        }
    }
    
    private List<DetectiveDto> GenerateSampleDetectives()
    {
        var sampleDetectives = new List<DetectiveDto>();
        var random = new Random();
        
        var firstNames = new[]
        {
            "Петро", "Оксана", "Максим", "Юлія", "Артем", "Вікторія", "Денис", "Катерина",
            "Роман", "Аліна", "Ігор", "Валентина", "Станіслав", "Галина", "Віталій", "Лариса",
            "Андрій", "Наталія", "Володимир", "Ірина", "Михайло", "Світлана", "Олександр", "Людмила",
            "Сергій", "Олена", "Дмитро", "Тетяна", "Олексій", "Анна"
        };
        
        var lastNames = new[]
        {
            "Детективов", "Слідча", "Розшуковець", "Слідчий", "Детектив", "Розшук", "Детективка", "Слідча",
            "Петренко", "Коваленко", "Сидоренко", "Мельник", "Бондаренко", "Шевченко", "Кравченко", "Морозенко",
            "Гриценко", "Левченко", "Козленко", "Савченко", "Романенко", "Ткаченко", "Гончаренко", "Федоренко",
            "Коваль", "Мороз", "Сидор", "Петренко", "Кравченко", "Шевченко"
        };
        
        var statuses = new[] { DetectiveStatus.Active, DetectiveStatus.OnVacation, DetectiveStatus.Retired, DetectiveStatus.Fired };
        var domains = new[] { "detective.ua", "investigation.com", "detective.gov.ua", "police.ua" };
        
        for (int i = 1; i <= 20; i++)
        {
            var firstName = firstNames[random.Next(firstNames.Length)];
            var lastName = lastNames[random.Next(lastNames.Length)];
            var hireDate = DateTime.Now.AddDays(-random.Next(30, 1825)); // Last 5 years
            
            sampleDetectives.Add(new DetectiveDto
            {
                Id = Detectives.Count + i,
                FirstName = firstName,
                LastName = lastName,
                FatherName = random.Next(2) == 0 ? "Петрович" : null,
                Status = statuses[random.Next(statuses.Length)],
                HireDate = hireDate,
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@{domains[random.Next(domains.Length)]}",
                PhoneNumber = $"+380{random.Next(10, 99)}{random.Next(1000000, 9999999)}",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-random.Next(25, 60))),
                Region = "Київська область",
                City = "Київ",
                Street = "Вулиця Детективна",
                BuildingNumber = random.Next(1, 100).ToString(),
                ApartmentNumber = random.Next(1, 50),
                Salary = random.Next(15000, 50000)
            });
        }
        
        return sampleDetectives;
    }
}


