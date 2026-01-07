using CaseFlow.DAL.Enums;

namespace CaseFlow.BLL.Dto.Evidence;

public class EvidenceCaseDto
{
    public int EvidenceId { get; set; }
    public int? CaseId { get; set; }
    
    public EvidenceType Type { get; set; }
    public string Description { get; set; } = null!;
    public DateTime CollectionDate { get; set; }
    public string Region { get; set; } = "Не вказано";
    public string? Annotation { get; set; }
    public string? Purpose { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;
    
}