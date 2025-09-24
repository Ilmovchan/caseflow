using CaseFlow.DAL.Enums;

namespace CaseFlow.BLL.Dto.Case;

public class CaseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly DeadlineDate { get; set; }
    public DateOnly? CloseDate { get; set; }
    public CaseStatus Status { get; set; }
    
    // Foreign key IDs
    public int CaseTypeId { get; set; }
    public int ClientId { get; set; }
    public int? DetectiveId { get; set; }
    
    // Human-readable names
    public string CaseTypeName { get; set; } = null!;
    public string ClientFullName { get; set; } = null!;
    public string? DetectiveFullName { get; set; }
}