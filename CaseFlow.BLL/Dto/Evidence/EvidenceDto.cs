using CaseFlow.DAL.Enums;

namespace CaseFlow.BLL.Dto.Evidence;

public class EvidenceDto
{
    public int Id { get; set; }
    public EvidenceType Type { get; set; }
    public string Description { get; set; } = null!;
    public DateTime CollectionDate { get; set; }
    public string Region { get; set; } = "Не вказано";
    public string? Annotation { get; set; }
    public string? Purpose { get; set; } 
    
}