using AutoMapper;
using CaseFlow.BLL.Dto.Client;
using CaseFlow.BLL.Dto.Common;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using CaseFlow.PAGES.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Clients;

[Authorize(Policy = "DetectiveOnly")]
public class IndexModel(DetectiveService detectiveService, IMapper mapper) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;
    private readonly IMapper _mapper = mapper;

    public List<ClientDto> Clients { get; set; } = new();
    public PagedResult<ClientDto> PagedClients { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(int pageNumber = 1)
    {
        CurrentPage = pageNumber;
        var identity = DetectiveIdentity.FromUser(User);
        var raw = string.IsNullOrEmpty(identity)
            ? new List<Client>()
            : await _detectiveService.GetClientsForDetectiveAsync(identity);

        var clientDtos = _mapper.Map<List<ClientDto>>(raw);

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            clientDtos = clientDtos.Where(c =>
                c.Id.ToString().Contains(term) ||
                c.FullName.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                c.PhoneNumber.Contains(term) ||
                c.City.ToLower().Contains(term))
                .ToList();
        }

        var totalCount = clientDtos.Count;
        var items = clientDtos
            .OrderBy(c => c.Id)
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        PagedClients = new PagedResult<ClientDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = PageSize
        };

        Clients = PagedClients.Items;
    }
}
