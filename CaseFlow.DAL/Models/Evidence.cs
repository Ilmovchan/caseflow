using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Interfaces;

namespace CaseFlow.DAL.Models;

[Table("evidence")]
public class Evidence : IWorkflowEntity
{
    [Column("id")]
    public int Id { get; set; }

    [Column("description")]
    [MaxLength(200)]
    public string Description { get; set; } = null!;

    [Column("type")]
    public EvidenceType Type { get; set; }

    [Column("collection_date")]
    public DateTime CollectionDate { get; set; }

    [Column("region")] 
    [MaxLength(200)] 
    public string Region { get; set; } = "Не вказано";
    
    [Column("annotation")]
    [MaxLength(200)]
    public string? Annotation { get; set; }
    
    [Column("purpose")]
    [MaxLength(200)]
    public string? Purpose { get; set; }

    [Column("approval_status")]
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;

    public ICollection<CaseEvidence> CaseEvidences { get; set; } = new List<CaseEvidence>();
}