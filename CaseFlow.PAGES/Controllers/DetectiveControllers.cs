using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace CaseFlow.API.Controllers;

// ---------------- CASE ----------------
[ApiController]
[Route("api/detective/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class DetectiveCaseController(DetectiveService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var item = await service.GetCaseAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await service.GetCasesAsync());

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCaseByDetectiveDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var updated = await service.UpdateCaseAsync(id, dto);
        return Ok(updated);
    }
}

// ---------------- CLIENT ----------------
[ApiController]
[Route("api/detective/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class DetectiveClientController(DetectiveService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var item = await service.GetClientAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await service.GetClientsAsync());
}

// ---------------- EVIDENCE ----------------
[ApiController]
[Route("api/detective/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class DetectiveEvidenceController(DetectiveService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var item = await service.GetEvidenceAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await service.GetEvidencesAsync());

    [HttpGet("case/{caseId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFromCase(int caseId) => Ok(await service.GetEvidencesFromCase(caseId));

    [HttpGet("approved")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApproved() => Ok(await service.GetApprovedEvidencesAsync());

    [HttpGet("declined")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeclined() => Ok(await service.GetDeclinedEvidencesAsync());

    [HttpGet("pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending() => Ok(await service.GetPendingEvidencesAsync());

    [HttpPost("case/{caseId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(int caseId, [FromBody] CreateEvidenceDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await service.CreateEvidenceAsync(caseId, dto);
        return CreatedAtAction(nameof(Get), new { id = created.EvidenceId }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEvidenceDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var updated = await service.UpdateEvidenceAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteEvidenceAsync(id);
        return NoContent();
    }

    [HttpPost("{evidenceId:int}/link/{caseId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Link(int evidenceId, int caseId)
    {
        await service.LinkEvidenceToCaseAsync(evidenceId, caseId);
        return NoContent();
    }

    [HttpPost("{evidenceId:int}/unlink/{caseId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unlink(int evidenceId, int caseId)
    {
        await service.UnlinkEvidenceFromCaseAsync(evidenceId, caseId);
        return NoContent();
    }
}

// ---------------- SUSPECT ----------------
[ApiController]
[Route("api/detective/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class DetectiveSuspectController(DetectiveService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var item = await service.GetSuspectAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await service.GetSuspectsAsync());

    [HttpGet("case/{caseId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFromCase(int caseId) => Ok(await service.GetSuspectsFromCase(caseId));

    [HttpGet("approved")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApproved() => Ok(await service.GetApprovedSuspectsAsync());

    [HttpGet("declined")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeclined() => Ok(await service.GetDeclinedSuspectsAsync());

    [HttpGet("pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending() => Ok(await service.GetPendingSuspectsAsync());

    [HttpPost("case/{caseId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(int caseId, [FromBody] CreateSuspectDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await service.CreateSuspectAsync(caseId, dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSuspectDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var updated = await service.UpdateSuspectAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteSuspectAsync(id);
        return NoContent();
    }

    [HttpPost("{suspectId:int}/link/{caseId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Link(int suspectId, int caseId)
    {
        await service.LinkSuspectToCaseAsync(suspectId, caseId);
        return NoContent();
    }

    [HttpPost("{suspectId:int}/unlink/{caseId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unlink(int suspectId, int caseId)
    {
        await service.UnlinkSuspectFromCaseAsync(suspectId, caseId);
        return NoContent();
    }
}

// ---------------- EXPENSE ----------------
[ApiController]
[Route("api/detective/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class DetectiveExpenseController(DetectiveService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var item = await service.GetExpenseAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await service.GetExpensesAsync());

    [HttpGet("case/{caseId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFromCase(int caseId) => Ok(await service.GetExpensesFromCaseAsync(caseId));

    [HttpGet("approved")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApproved() => Ok(await service.GetApprovedExpensesAsync());

    [HttpGet("declined")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeclined() => Ok(await service.GetDeclinedExpensesAsync());

    [HttpGet("pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending() => Ok(await service.GetPendingExpensesAsync());

    [HttpPost("case/{caseId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(int caseId, [FromBody] CreateExpenseDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await service.CreateExpenseAsync(caseId, dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExpenseDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var updated = await service.UpdateExpenseAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteExpenseAsync(id);
        return NoContent();
    }
}

// ---------------- REPORT ----------------
[ApiController]
[Route("api/detective/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class DetectiveReportController(DetectiveService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var item = await service.GetReportAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await service.GetReportsAsync());

    [HttpGet("case/{caseId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFromCase(int caseId) => Ok(await service.GetReportsFromCaseAsync(caseId));

    [HttpGet("approved")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApproved() => Ok(await service.GetApprovedReportsAsync());

    [HttpGet("declined")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeclined() => Ok(await service.GetDeclinedReportsAsync());

    [HttpGet("pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending() => Ok(await service.GetPendingReportsAsync());

    [HttpPost("case/{caseId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(int caseId, [FromBody] CreateReportDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await service.CreateReportAsync(caseId, dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReportDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var updated = await service.UpdateReportAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteReportAsync(id);
        return NoContent();
    }
}
