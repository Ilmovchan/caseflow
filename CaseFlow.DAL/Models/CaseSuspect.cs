using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaseFlow.DAL.Models;

[Table("case_suspect")]
public class CaseSuspect
{
    [Column("suspect_id")]
    public int SuspectId { get; set; }
    
    [Column("case_id")]
    public int CaseId { get; set; }

    [Column("is_interrogated")]
    public bool IsInterrogated { get; set; }

    [Column("alibi")]
    public string? Alibi { get; set; }
    
    public virtual Case Case { get; set; } = null!;
    
    public virtual Suspect Suspect { get; set; } = null!;
}