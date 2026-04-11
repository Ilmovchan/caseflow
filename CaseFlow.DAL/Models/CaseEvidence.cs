using System.ComponentModel.DataAnnotations.Schema;

namespace CaseFlow.DAL.Models;

[Table("case_evidence")]
public class CaseEvidence
{
    [Column("evidence_id")]
    public int EvidenceId { get; set; }
    
    [Column("case_id")]
    public int CaseId { get; set; }
    
    public virtual Case Case { get; set; } = null!;
    public virtual Evidence Evidence { get; set; } = null!;
}