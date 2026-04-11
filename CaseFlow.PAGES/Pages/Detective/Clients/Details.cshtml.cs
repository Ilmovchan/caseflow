using AutoMapper;
using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Models;
using CaseFlow.PAGES.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CaseFlow.PAGES.Pages.Detective.Clients;

[Authorize(Policy = "DetectiveOnly")]
public class DetailsModel(DetectiveService detectiveService, IMapper mapper) : PageModel
{
    private readonly DetectiveService _detectiveService = detectiveService;
    private readonly IMapper _mapper = mapper;

    public Client Client { get; set; } = null!;
    public List<CaseDto> ConnectedCases { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();

        var entity = await _detectiveService.GetClientForDetectiveAsync(id, identity);
        if (entity == null)
            return NotFound();

        Client = entity;

        var myCases = await _detectiveService.GetCasesByDetectiveEmailAsync(identity);
        ConnectedCases = _mapper.Map<List<CaseDto>>(myCases.Where(c => c.ClientId == id).OrderBy(c => c.Id).ToList());

        return Page();
    }
}
