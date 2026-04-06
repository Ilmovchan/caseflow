using AutoMapper;
using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CaseFlow.PAGES.Pages.Admin.Evidence;

[IgnoreAntiforgeryToken]
public class IndexModel(AdminService adminService, IMapper mapper) : PageModel
{
    private readonly AdminService _adminService = adminService;
    private readonly IMapper _mapper = mapper;

    public List<EvidenceDto> AllEvidence { get; set; } = new();
    public List<EvidenceDto> PendingEvidence { get; set; } = new();
    public PagedResult<EvidenceDto> PagedEvidence { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;

        // Get all evidence first
        List<CaseFlow.DAL.Models.Evidence> allEvidence;
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchResult = await _adminService.SearchEvidencesPagedAsync(SearchTerm, 1, int.MaxValue);
            allEvidence = searchResult.Items;
        }
        else
        {
            var allResult = await _adminService.GetEvidencesPagedAsync(1, int.MaxValue);
            allEvidence = allResult.Items;
        }

        // Filter: Admin sees submitted workflow items (exclude Draft only)
        var filteredItems = allEvidence
            .Where(e => e.ApprovalStatus != ApprovalStatus.Draft)
            .ToList();

        // Apply pagination to filtered items
        var totalCount = filteredItems.Count;
        var items = filteredItems
            .OrderBy(e => e.Id)
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        PagedEvidence = new PagedResult<EvidenceDto>
        {
            Items = _mapper.Map<List<EvidenceDto>>(items),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = PageSize
        };

        AllEvidence = PagedEvidence.Items;

        // Get pending evidence for approval section
        var pendingEvidenceEntities = await _adminService.GetPendingEvidencesAsync();
        PendingEvidence = _mapper.Map<List<EvidenceDto>>(pendingEvidenceEntities);
    }
    
    public async Task<IActionResult> OnPostApproveAsync([FromQuery] int id)
    {
        try
        {
            var approved = await _adminService.ApproveEvidenceAsync(id);
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
            var rejected = await _adminService.RejectEvidenceAsync(id);
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
            var submitted = await _adminService.SubmitEvidenceAsync(id);
            return new JsonResult(new {
                success = true,
                newStatus = "Pending",
                newStatusText = "Очікує",
                newStatusColor = "warning",
                approvalStatus = "Pending",
                message = "Доказ надіслано на перевірку!",
                // keep response lightweight (frontend only needs status fields)
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
            await _adminService.DeleteEvidenceAsync(id);
            return new JsonResult(new {
                success = true,
                message = "Доказ видалено успішно"
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
    
    private List<EvidenceDto> GenerateSampleEvidence()
    {
        var sampleEvidence = new List<EvidenceDto>();
        var random = new Random();
        
        var evidenceTypes = new[] { EvidenceType.Document, EvidenceType.Photo, EvidenceType.Video, EvidenceType.Biometric, EvidenceType.Biological };
        
        var descriptions = new[]
        {
            "Знайдено на місці злочину", "Вилучено у підозрюваного", "Отримано від свідка", "Знайдено в автомобілі",
            "Вилучено з квартири", "Знайдено на вулиці", "Отримано з банку", "Вилучено з офісу",
            "Знайдено в лісі", "Отримано з лабораторії", "Вилучено з магазину", "Знайдено в парку",
            "Отримано з лікарні", "Вилучено з школи", "Знайдено в ресторані", "Отримано з готелю",
            "Вилучено з пошти", "Знайдено в поїзді", "Отримано з літака", "Вилучено з корабля",
            "Знайдено в підвалі", "Отримано з горища", "Вилучено з гаражу", "Знайдено в сараї",
            "Отримано з саду", "Вилучено з поля", "Знайдено в річці", "Отримано з озера",
            "Вилучено з моря", "Знайдено в горах", "Отримано з печери", "Вилучено з шахти"
        };
        
        var regions = new[]
        {
            "Київська", "Харківська", "Одеська", "Дніпропетровська", "Донецька", "Запорізька", "Львівська", "Криворізька",
            "Миколаївська", "Маріупольська", "Луганська", "Вінницька", "Херсонська", "Полтавська", "Чернігівська", "Черкаська",
            "Сумська", "Хмельницька", "Чернівецька", "Житомирська", "Кіровоградська", "Рівненська", "Івано-Франківська", "Тернопільська"
        };
        
        var approvalStatuses = new[] { ApprovalStatus.Approved, ApprovalStatus.Pending, ApprovalStatus.Declined };
        
        for (int i = 1; i <= 45; i++)
        {
            var collectionDate = DateTime.Now.AddDays(-random.Next(1, 365)); // Last year
            var approvalStatus = approvalStatuses[random.Next(approvalStatuses.Length)];
            
            sampleEvidence.Add(new EvidenceDto
            {
                Id = AllEvidence.Count + i,
                Type = evidenceTypes[random.Next(evidenceTypes.Length)],
                Description = descriptions[random.Next(descriptions.Length)],
                CollectionDate = collectionDate,
                Region = regions[random.Next(regions.Length)],
                ApprovalStatus = approvalStatus
            });
        }
        
        return sampleEvidence;
    }
}
