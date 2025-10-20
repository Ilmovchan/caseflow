using AutoMapper;
using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.CaseType;
using CaseFlow.BLL.Dto.Client;
using CaseFlow.BLL.Dto.Detective;
using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace CaseFlow.PAGES.Controllers;

// ---------------- CASE ----------------
[ApiController]
[Route("api/admin/case")]
[Produces("application/json")]
[Consumes("application/json")]
public class AdminCaseController(AdminService service) : ControllerBase
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

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCaseDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await service.CreateCaseAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCaseByAdminDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var updated = await service.UpdateCaseAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteCaseAsync(id);
        return NoContent();
    }

    [HttpPost("{caseId:int}/assign/{detectiveId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignDetective(int caseId, int detectiveId)
        => Ok(await service.AssignDetectiveAsync(caseId, detectiveId));

    [HttpPost("{caseId:int}/dismiss")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DismissDetective(int caseId)
    {
        await service.DismissDetectiveAsync(caseId);
        return NoContent();
    }
}

// ---------------- CLIENT ----------------
[ApiController]
[Route("api/admin/client")]
[Produces("application/json")]
[Consumes("application/json")]
public class AdminClientController(AdminService service) : ControllerBase
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

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateClientDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await service.CreateClientAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClientDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var updated = await service.UpdateClientAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteClientAsync(id);
        return NoContent();
    }
}

// ---------------- DETECTIVE ----------------
[ApiController]
[Route("api/admin/detective")]
[Produces("application/json")]
[Consumes("application/json")]
public class AdminDetectiveController(AdminService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var item = await service.GetDetectiveAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await service.GetDetectivesAsync());

    [HttpGet("unassigned")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnassigned() => Ok(await service.GetUnassignedDetectivesAsync());

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDetectiveDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await service.CreateDetectiveAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDetectiveDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var updated = await service.UpdateDetectiveAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteDetectiveAsync(id);
        return NoContent();
    }
}

// ---------------- EVIDENCE ----------------
[ApiController]
[Route("api/admin/evidence")]
[Produces("application/json")]
[Consumes("application/json")]
public class AdminEvidenceController(AdminService service, IMapper mapper) : ControllerBase
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
    public async Task<IActionResult> GetFromCase(int caseId) => Ok(await service.GetEvidencesFromCaseAsync(caseId));

    [HttpGet("pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending() => Ok(await service.GetPendingEvidencesAsync());

    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(int id)
    {
        var approved = await service.ApproveEvidenceAsync(id);
        var evidenceDto = mapper.Map<EvidenceDto>(approved);
        evidenceDto.ApprovalStatus = DAL.Enums.ApprovalStatus.Approved;
        return Ok(new {
            success = true,
            newStatus = "Approved",
            newStatusText = "Схвалено",
            newStatusColor = "success",
            approvalStatus = "Approved",
            evidence = evidenceDto
        });
    }

    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(int id)
    {
        var rejected = await service.RejectEvidenceAsync(id);
        var evidenceDto = mapper.Map<EvidenceDto>(rejected);
        evidenceDto.ApprovalStatus = DAL.Enums.ApprovalStatus.Declined;
        return Ok(new {
            success = true,
            newStatus = "Declined",
            newStatusText = "Відхилено",
            newStatusColor = "danger",
            approvalStatus = "Declined",
            evidence = evidenceDto
        });
    }

    // Evidence approval methods removed - Evidence entity no longer has ApprovalStatus
    // Approval status is managed through CaseEvidence junction table
}

// ---------------- SUSPECT ----------------
[ApiController]
[Route("api/admin/suspect")]
[Produces("application/json")]
[Consumes("application/json")]
public class AdminSuspectController(AdminService service, IMapper mapper) : ControllerBase
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
    public async Task<IActionResult> GetFromCase(int caseId) => Ok(await service.GetSuspectsFromCaseAsync(caseId));

    [HttpGet("pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending() => Ok(await service.GetPendingSuspectsAsync());

    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(int id)
    {
        var approved = await service.ApproveSuspectAsync(id);
        var suspectDto = mapper.Map<SuspectDto>(approved);
        suspectDto.ApprovalStatus = DAL.Enums.ApprovalStatus.Approved;
        return Ok(new {
            success = true,
            newStatus = "Approved",
            newStatusText = "Схвалено",
            newStatusColor = "success",
            approvalStatus = "Approved",
            suspect = suspectDto
        });
    }

    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(int id)
    {
        var rejected = await service.RejectSuspectAsync(id);
        var suspectDto = mapper.Map<SuspectDto>(rejected);
        suspectDto.ApprovalStatus = DAL.Enums.ApprovalStatus.Declined;
        return Ok(new {
            success = true,
            newStatus = "Declined",
            newStatusText = "Відхилено",
            newStatusColor = "danger",
            approvalStatus = "Declined",
            suspect = suspectDto
        });
    }

    // Suspect approval methods removed - Suspect entity no longer has ApprovalStatus
    // Approval status is managed through CaseSuspect junction table
}

// ---------------- EXPENSE ----------------
[ApiController]
[Route("api/admin/expense")]
[Produces("application/json")]
[Consumes("application/json")]
public class AdminExpenseController(AdminService service) : ControllerBase
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

    [HttpGet("pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending() => Ok(await service.GetPendingExpensesAsync());

    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(int id)
    {
        var approved = await service.ApproveExpenseAsync(id);
        return Ok(new { 
            success = true, 
            newStatus = "Approved", 
            newStatusText = "Схвалено", 
            newStatusColor = "success",
            approvalStatus = "Approved",
            expense = approved 
        });
    }

    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(int id)
    {
        var rejected = await service.RejectExpenseAsync(id);
        return Ok(new { 
            success = true, 
            newStatus = "Declined", 
            newStatusText = "Відхилено", 
            newStatusColor = "danger",
            approvalStatus = "Declined",
            expense = rejected 
        });
    }
}

// ---------------- REPORT ----------------
[ApiController]
[Route("api/admin/report")]
[Produces("application/json")]
[Consumes("application/json")]
public class AdminReportController(AdminService service, IMapper mapper) : ControllerBase
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

    [HttpGet("pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending() => Ok(await service.GetPendingReportsAsync());

    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(int id)
    {
        var approved = await service.ApproveReportAsync(id);
        var reportDto = mapper.Map<ReportDto>(approved);
        reportDto.ApprovalStatus = DAL.Enums.ApprovalStatus.Approved;
        return Ok(new {
            success = true,
            newStatus = "Approved",
            newStatusText = "Схвалено",
            newStatusColor = "success",
            approvalStatus = "Approved",
            report = reportDto
        });
    }

    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(int id)
    {
        var rejected = await service.RejectReportAsync(id);
        var reportDto = mapper.Map<ReportDto>(rejected);
        reportDto.ApprovalStatus = DAL.Enums.ApprovalStatus.Declined;
        return Ok(new {
            success = true,
            newStatus = "Declined",
            newStatusText = "Відхилено",
            newStatusColor = "danger",
            approvalStatus = "Declined",
            report = reportDto
        });
    }
}

// ---------------- CASE TYPE ----------------
[ApiController]
[Route("api/admin/casetype")]
[Produces("application/json")]
[Consumes("application/json")]
public class AdminCaseTypeController(AdminService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var item = await service.GetCaseTypeAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await service.GetCaseTypesAsync());

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCaseTypeDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await service.CreateCaseTypeAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCaseTypeDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var updated = await service.UpdateCaseTypeAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteCaseTypeAsync(id);
        return NoContent();
    }
}
