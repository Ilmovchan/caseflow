using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CaseFlow.DAL.Enums;
using CaseFlow.DAL.Interfaces;

namespace CaseFlow.DAL.Models;

[Table("report")]
public class Report : IWorkflowEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("case_id")]
    public int CaseId { get; set; }

    [Column("report_date")]
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;

    [Column("summary")]
    public string Summary { get; set; } = null!;

    [Column("comments")]
    public string? Comments { get; set; }
    
    [Column("approval_status")] 
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;

    [Column("created_by_detective_id")]
    public int? CreatedByDetectiveId { get; set; }

    public Detective? CreatedByDetective { get; set; }

    public Case Case { get; set; } = null!;
}