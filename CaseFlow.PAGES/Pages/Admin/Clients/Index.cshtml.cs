using AutoMapper;
using CaseFlow.BLL.Dto.Client;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Admin.Clients;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<ClientDto> Clients { get; set; } = new();
    public PagedResult<ClientDto> PagedClients { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 15;

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;
        var pagedResult = await _adminService.GetClientsPagedAsync(pageNumber, PageSize);
        PagedClients = new PagedResult<ClientDto>
        {
            Items = _mapper.Map<List<ClientDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };
        
        Clients = PagedClients.Items;
        
        // Add sample data if we have less than 10 clients
        if (Clients.Count < 10)
        {
            Clients.AddRange(GenerateSampleClients());
            // Re-sort the combined list to maintain ID order
            Clients = Clients.OrderBy(c => c.Id).ToList();
        }
    }
    
    private List<ClientDto> GenerateSampleClients()
    {
        var sampleClients = new List<ClientDto>();
        var random = new Random();
        
        var firstNames = new[]
        {
            "Іван", "Марія", "Олексій", "Анна", "Дмитро", "Тетяна", "Сергій", "Олена",
            "Андрій", "Наталія", "Володимир", "Ірина", "Михайло", "Світлана", "Олександр", "Людмила",
            "Петро", "Катерина", "Максим", "Юлія", "Артем", "Вікторія", "Денис", "Аліна",
            "Роман", "Валентина", "Станіслав", "Галина", "Віталій", "Лариса"
        };
        
        var lastNames = new[]
        {
            "Петренко", "Коваленко", "Сидоренко", "Мельник", "Бондаренко", "Шевченко", "Кравченко", "Морозенко",
            "Гриценко", "Левченко", "Козленко", "Савченко", "Романенко", "Ткаченко", "Гончаренко", "Федоренко",
            "Детективов", "Слідча", "Розшуковець", "Слідчий", "Детектив", "Розшук", "Детективка", "Слідча",
            "Коваль", "Мороз", "Сидор", "Петренко", "Кравченко", "Шевченко"
        };
        
        var cities = new[]
        {
            "Київ", "Харків", "Одеса", "Дніпро", "Донецьк", "Запоріжжя", "Львів", "Кривий Ріг",
            "Миколаїв", "Маріуполь", "Луганськ", "Вінниця", "Херсон", "Полтава", "Чернігів", "Черкаси",
            "Суми", "Хмельницький", "Чернівці", "Житомир", "Кропивницький", "Рівне", "Івано-Франківськ", "Тернопіль"
        };
        
        var domains = new[] { "gmail.com", "ukr.net", "mail.ua", "i.ua", "meta.ua", "bigmir.net" };
        
        // Start with ID 1000 to avoid conflicts with real data
        int startId = 1000;
        for (int i = 1; i <= 30; i++)
        {
            var firstName = firstNames[random.Next(firstNames.Length)];
            var lastName = lastNames[random.Next(lastNames.Length)];
            var registrationDate = DateTime.Now.AddDays(-random.Next(1, 1095)); // Last 3 years
            
            sampleClients.Add(new ClientDto
            {
                Id = startId + i, // Sequential IDs starting from 1001
                FirstName = firstName,
                LastName = lastName,
                FatherName = random.Next(2) == 0 ? "Петрович" : null,
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@{domains[random.Next(domains.Length)]}",
                PhoneNumber = $"+380{random.Next(10, 99)}{random.Next(1000000, 9999999)}",
                City = cities[random.Next(cities.Length)],
                Region = "Київська область",
                Street = "Вулиця Центральна",
                BuildingNumber = random.Next(1, 100).ToString(),
                ApartmentNumber = random.Next(1, 50),
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-random.Next(18, 65))),
                RegistrationDate = registrationDate
            });
        }
        
        return sampleClients;
    }
}


