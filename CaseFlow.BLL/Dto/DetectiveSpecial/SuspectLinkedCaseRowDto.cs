using CaseFlow.DAL.Enums;

namespace CaseFlow.BLL.Dto.DetectiveSpecial;

public sealed class SuspectLinkedCaseRowDto
{
    public int CaseId { get; init; }
    public string Title { get; init; } = "";
    public CaseStatus Status { get; init; }
}
