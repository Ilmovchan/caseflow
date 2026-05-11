using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Exceptions;
using CaseFlow.BLL.Services;
using CaseFlow.PAGES.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaseFlow.PAGES.Controllers;

[ApiController]
[Route("api/detective/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
[Authorize(Policy = "DetectiveOnly")]
public class DetectiveCaseController(DetectiveService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(int id)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        var item = await service.GetCaseForDetectiveAsync(id, identity);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetCasesByDetectiveEmailAsync(identity));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCaseByDetectiveDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        try
        {
            var updated = await service.UpdateCaseAsync(id, dto, identity);
            return Ok(updated);
        }
        catch (CaseFlow.BLL.Exceptions.EntityNotFoundException)
        {
            return NotFound();
        }
    }
}

[ApiController]
[Route("api/detective/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
[Authorize(Policy = "DetectiveOnly")]
public class DetectiveClientController(DetectiveService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        var item = await service.GetClientForDetectiveAsync(id, identity);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetClientsForDetectiveAsync(identity));
    }
}

[ApiController]
[Route("api/detective/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
[Authorize(Policy = "DetectiveOnly")]
public class DetectiveEvidenceController(DetectiveService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        var item = await service.GetEvidenceAsync(id, identity);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetEvidencesAsync(identity));
    }

    [HttpGet("linkable/case/{caseId:int}")]
    public async Task<IActionResult> GetLinkableForCase(int caseId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetEvidencesLinkableToCaseAsync(caseId, identity));
    }

    [HttpGet("case/{caseId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFromCase(int caseId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetEvidencesFromCase(caseId, identity));
    }

    [HttpGet("approved")]
    public async Task<IActionResult> GetApproved()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetApprovedEvidencesAsync(identity));
    }

    [HttpGet("declined")]
    public async Task<IActionResult> GetDeclined()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetDeclinedEvidencesAsync(identity));
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetPendingEvidencesAsync(identity));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateEvidenceDto dto, [FromQuery] bool draft = false)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        var created = await service.CreateEvidenceAsync(dto, identity, submitForApproval: !draft);
        return CreatedAtAction(nameof(Get), new { id = created.EvidenceId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEvidenceDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        var updated = await service.UpdateEvidenceAsync(id, dto, identity);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        await service.DeleteEvidenceAsync(id, identity);
        return NoContent();
    }

    [HttpPost("{evidenceId:int}/link/{caseId:int}")]
    public async Task<IActionResult> Link(int evidenceId, int caseId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        await service.LinkEvidenceToCaseAsync(evidenceId, caseId, identity);
        return NoContent();
    }

    [HttpPost("{evidenceId:int}/unlink/{caseId:int}")]
    public async Task<IActionResult> Unlink(int evidenceId, int caseId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        await service.UnlinkEvidenceFromCaseAsync(evidenceId, caseId, identity);
        return NoContent();
    }
}

[ApiController]
[Route("api/detective/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
[Authorize(Policy = "DetectiveOnly")]
public class DetectiveSuspectController(DetectiveService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        var item = await service.GetSuspectAsync(id, identity);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetSuspectsAsync(identity));
    }

    [HttpGet("linkable/case/{caseId:int}")]
    public async Task<IActionResult> GetLinkableForCase(int caseId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetSuspectsLinkableToCaseAsync(caseId, identity));
    }

    [HttpGet("case/{caseId:int}")]
    public async Task<IActionResult> GetFromCase(int caseId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetSuspectsFromCase(caseId, identity));
    }

    [HttpGet("approved")]
    public async Task<IActionResult> GetApproved()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetApprovedSuspectsAsync(identity));
    }

    [HttpGet("declined")]
    public async Task<IActionResult> GetDeclined()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetDeclinedSuspectsAsync(identity));
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetPendingSuspectsAsync(identity));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSuspectDto dto, [FromQuery] bool draft = false)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        try
        {
            var created = await service.CreateSuspectAsync(dto, identity, submitForApproval: !draft);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (SuspectValidationException ex)
        {
            foreach (var (prop, msg) in ex.Errors)
                ModelState.AddModelError(prop, msg);
            return ValidationProblem(ModelState);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSuspectDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        var updated = await service.UpdateSuspectAsync(id, dto, identity);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        await service.DeleteSuspectAsync(id, identity);
        return NoContent();
    }

    [HttpPost("{suspectId:int}/link/{caseId:int}")]
    public async Task<IActionResult> Link(int suspectId, int caseId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        await service.LinkSuspectToCaseAsync(suspectId, caseId, identity);
        return NoContent();
    }

    [HttpPost("{suspectId:int}/unlink/{caseId:int}")]
    public async Task<IActionResult> Unlink(int suspectId, int caseId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        await service.UnlinkSuspectFromCaseAsync(suspectId, caseId, identity);
        return NoContent();
    }
}

[ApiController]
[Route("api/detective/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
[Authorize(Policy = "DetectiveOnly")]
public class DetectiveExpenseController(DetectiveService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        var item = await service.GetExpenseAsync(id, identity);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetExpensesAsync(identity));
    }

    [HttpGet("case/{caseId:int}")]
    public async Task<IActionResult> GetFromCase(int caseId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetExpensesFromCaseAsync(caseId, identity));
    }

    [HttpGet("approved")]
    public async Task<IActionResult> GetApproved()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetApprovedExpensesAsync(identity));
    }

    [HttpGet("declined")]
    public async Task<IActionResult> GetDeclined()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetDeclinedExpensesAsync(identity));
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetPendingExpensesAsync(identity));
    }

    [HttpPost("case/{caseId:int}")]
    public async Task<IActionResult> Create(int caseId, [FromBody] CreateExpenseDto dto, [FromQuery] bool draft = false)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        var created = await service.CreateExpenseAsync(caseId, dto, identity, submitForApproval: !draft);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExpenseDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        var updated = await service.UpdateExpenseAsync(id, dto, identity);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        await service.DeleteExpenseAsync(id, identity);
        return NoContent();
    }
}

[ApiController]
[Route("api/detective/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
[Authorize(Policy = "DetectiveOnly")]
public class DetectiveReportController(DetectiveService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        var item = await service.GetReportAsync(id, identity);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetReportsAsync(identity));
    }

    [HttpGet("case/{caseId:int}")]
    public async Task<IActionResult> GetFromCase(int caseId)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetReportsFromCaseAsync(caseId, identity));
    }

    [HttpGet("approved")]
    public async Task<IActionResult> GetApproved()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetApprovedReportsAsync(identity));
    }

    [HttpGet("declined")]
    public async Task<IActionResult> GetDeclined()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetDeclinedReportsAsync(identity));
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        return Ok(await service.GetPendingReportsAsync(identity));
    }

    [HttpPost("case/{caseId:int}")]
    public async Task<IActionResult> Create(int caseId, [FromBody] CreateReportDto dto, [FromQuery] bool draft = false)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        var created = await service.CreateReportAsync(caseId, dto, identity, submitForApproval: !draft);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReportDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        var updated = await service.UpdateReportAsync(id, dto, identity);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var identity = DetectiveIdentity.FromUser(User);
        if (string.IsNullOrEmpty(identity))
            return Unauthorized();
        await service.DeleteReportAsync(id, identity);
        return NoContent();
    }
}
