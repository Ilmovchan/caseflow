using AutoMapper;
using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CaseFlow.PAGES.Pages.Admin.Suspects;

[IgnoreAntiforgeryToken]
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

        List<Suspect> allSuspects;
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchResult = await _adminService.SearchSuspectsPagedAsync(SearchTerm, 1, int.MaxValue);
            allSuspects = searchResult.Items;
        }
        else
        {
            var allResult = await _adminService.GetSuspectsPagedAsync(1, int.MaxValue);
            allSuspects = allResult.Items;
        }

        var totalCount = allSuspects.Count;
        var items = allSuspects
            .OrderBy(s => s.Id)
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        PagedSuspects = new PagedResult<SuspectDto>
        {
            Items = _mapper.Map<List<SuspectDto>>(items),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = PageSize
        };

        AllSuspects = PagedSuspects.Items;

        var pendingSuspectEntities = await _adminService.GetPendingSuspectsAsync();
        PendingSuspects = _mapper.Map<List<SuspectDto>>(pendingSuspectEntities);
    }
    
    public async Task<IActionResult> OnPostApproveAsync([FromQuery] int id)
    {
        try
        {
            var approved = await _adminService.ApproveSuspectAsync(id);
            return new JsonResult(new { 
                success = true, 
                newStatus = "Approved", 
                newStatusText = "Схвалено", 
                newStatusColor = "success",
                approvalStatus = "Approved"
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
    
    public async Task<IActionResult> OnPostRejectAsync([FromQuery] int id)
    {
        try
        {
            var rejected = await _adminService.RejectSuspectAsync(id);
            return new JsonResult(new {
                success = true,
                newStatus = "Declined",
                newStatusText = "Відхилено",
                newStatusColor = "danger",
                approvalStatus = "Declined"
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

    public async Task<IActionResult> OnPostSubmitAsync([FromQuery] int id)
    {
        try
        {
            var submitted = await _adminService.SubmitSuspectAsync(id);
            return new JsonResult(new {
                success = true,
                newStatus = "Pending",
                newStatusText = "Очікує",
                newStatusColor = "warning",
                approvalStatus = "Pending",
                message = "Підозрюваний надіслано на перевірку!",
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

    public async Task<IActionResult> OnPostDeleteAsync([FromQuery] int id)
    {
        try
        {
            await _adminService.DeleteSuspectAsync(id);
            return new JsonResult(new {
                success = true,
                message = "Підозрюваний видалено успішно"
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
