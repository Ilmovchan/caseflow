using AutoMapper;
using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace CaseFlow.PAGES.Pages.Admin.Suspects;

public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<SuspectDto> AllSuspects { get; set; } = new();
    public List<SuspectDto> PendingSuspects { get; set; } = new();
    public PagedResult<SuspectDto> PagedSuspects { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;

        PagedResult<Suspect> pagedResult;
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            pagedResult = await _adminService.SearchSuspectsPagedAsync(SearchTerm, pageNumber, PageSize);
        }
        else
        {
            pagedResult = await _adminService.GetSuspectsPagedAsync(pageNumber, PageSize);
        }

        PagedSuspects = new PagedResult<SuspectDto>
        {
            Items = _mapper.Map<List<SuspectDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };

        AllSuspects = PagedSuspects.Items;

        // Get pending suspects for approval section
        var pendingSuspectEntities = await _adminService.GetPendingSuspectsAsync();
        PendingSuspects = _mapper.Map<List<SuspectDto>>(pendingSuspectEntities);
    }
    
    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        try
        {
            var approved = await _adminService.ApproveSuspectAsync(id);
            return new JsonResult(new { 
                success = true, 
                newStatus = "Approved", 
                newStatusText = "Схвалено", 
                newStatusColor = "success",
                approvalStatus = "Approved",
                suspect = approved 
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
    
    public async Task<IActionResult> OnPostRejectAsync(int id)
    {
        try
        {
            var rejected = await _adminService.RejectSuspectAsync(id);
            return new JsonResult(new { 
                success = true, 
                newStatus = "Declined", 
                newStatusText = "Відхилено", 
                newStatusColor = "danger",
                approvalStatus = "Declined",
                suspect = rejected 
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
    
    private List<SuspectDto> GenerateSampleSuspects()
    {
        var sampleSuspects = new List<SuspectDto>();
        var random = new Random();
        
        var firstNames = new[]
        {
            "Олексій", "Марія", "Дмитро", "Анна", "Сергій", "Олена", "Андрій", "Наталія",
            "Володимир", "Ірина", "Михайло", "Світлана", "Олександр", "Людмила", "Петро", "Катерина",
            "Максим", "Юлія", "Артем", "Вікторія", "Денис", "Аліна", "Роман", "Валентина",
            "Станіслав", "Галина", "Віталій", "Лариса", "Ігор", "Тетяна"
        };
        
        var lastNames = new[]
        {
            "Підозрюваний", "Підозрювана", "Злочинець", "Злочинка", "Порушник", "Порушниця",
            "Петренко", "Коваленко", "Сидоренко", "Мельник", "Бондаренко", "Шевченко", "Кравченко", "Морозенко",
            "Гриценко", "Левченко", "Козленко", "Савченко", "Романенко", "Ткаченко", "Гончаренко", "Федоренко",
            "Коваль", "Мороз", "Сидор", "Петренко", "Кравченко", "Шевченко"
        };
        
        var nicknames = new[]
        {
            "Вовк", "Лис", "Ведмідь", "Орел", "Сокіл", "Тигр", "Леопард", "Пантера", "Рись", "Яструб",
            "Сокіл", "Орлан", "Канюк", "Сова", "Філін", "Ворон", "Крук", "Грач", "Сорока", "Ворона",
            "Горобець", "Синиця", "Дятел", "Жайворонок", "Соловей", "Чиж", "Щиголь", "Зяблик", "В'юрок", "Чечевиця"
        };
        
        var regions = new[]
        {
            "Київська", "Харківська", "Одеська", "Дніпропетровська", "Донецька", "Запорізька", "Львівська", "Криворізька",
            "Миколаївська", "Маріупольська", "Луганська", "Вінницька", "Херсонська", "Полтавська", "Чернігівська", "Черкаська",
            "Сумська", "Хмельницька", "Чернівецька", "Житомирська", "Кіровоградська", "Рівненська", "Івано-Франківська", "Тернопільська"
        };
        
        var approvalStatuses = new[] { ApprovalStatus.Approved, ApprovalStatus.Pending, ApprovalStatus.Declined };
        
        for (int i = 1; i <= 35; i++)
        {
            var firstName = firstNames[random.Next(firstNames.Length)];
            var lastName = lastNames[random.Next(lastNames.Length)];
            var approvalStatus = approvalStatuses[random.Next(approvalStatuses.Length)];
            
            sampleSuspects.Add(new SuspectDto
            {
                Id = AllSuspects.Count + i,
                FirstName = firstName,
                LastName = lastName,
                FatherName = random.Next(2) == 0 ? "Петрович" : null,
                Nickname = random.Next(3) == 0 ? nicknames[random.Next(nicknames.Length)] : null,
                PhoneNumber = random.Next(2) == 0 ? $"+380{random.Next(10, 99)}{random.Next(1000000, 9999999)}" : null,
                Region = regions[random.Next(regions.Length)],
                ApprovalStatus = approvalStatus
            });
        }
        
        return sampleSuspects;
    }
}
