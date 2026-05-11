using CaseFlow.DAL.Enums;

namespace CaseFlow.BLL.Dto.Expense;

public class ExpenseDto
{
    public int Id { get; init; }
    public int CaseId { get; set; }
    
    public DateTime DateTime { get; set; }
    public string Purpose { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? Annotation { get; set; }
    public ApprovalStatus? ApprovalStatus { get; set; }
    
    public string CaseTitle { get; set; } = null!;
}