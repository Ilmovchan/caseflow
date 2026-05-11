namespace CaseFlow.BLL.Dto.AdminSpecial;

public sealed class AdminReportRowDto
{
    public int ReportId { get; init; }
    public int CaseId { get; init; }
    public int? DetectiveId { get; init; }
    public string DetectivePib { get; init; } = "—";
    public DateTime ReportDate { get; init; }
    public string Summary { get; init; } = "";
    public string? Comments { get; init; }
}

public sealed class DetectiveClosedRankingRowDto
{
    public int Place { get; init; }
    public int DetectiveId { get; init; }
    public string DetectivePib { get; init; } = "";
    public int ClosedCasesCount { get; init; }
    public decimal SuccessPercent { get; init; }
    public double? AvgCompletionDays { get; init; }
}

public sealed class DetectiveYearRankingRowDto
{
    public int DetectiveId { get; init; }
    public string DetectivePib { get; init; } = "";
    public decimal ClosedPercent { get; init; }
    public int OpenIncompleteCount { get; init; }
    public decimal TotalCasesValue { get; init; }
    public int EvidenceCount { get; init; }
    public int SuspectsCount { get; init; }
}

public sealed class ClientUnfinishedRankRowDto
{
    public int Place { get; init; }
    public int ClientId { get; init; }
    public string ClientPib { get; init; } = "";
    public int UnfinishedCount { get; init; }
}

public sealed class ClientUnfinishedSummaryDto
{
    public int ClientId { get; init; }
    public string ClientPib { get; init; } = "";
    public int UnfinishedCount { get; init; }
}

public sealed class CaseTypeRankRowDto
{
    public int Place { get; init; }
    public string CaseTypeName { get; init; } = "";
    public int Count { get; init; }
}

public sealed class CaseExpenseRankRowDto
{
    public int Place { get; init; }
    public int CaseId { get; init; }
    public string CaseTitle { get; init; } = "";
    public string ClientPib { get; init; } = "";
    public string DetectivePib { get; init; } = "";
    public int ExpensesCount { get; init; }
}

public sealed class FirstTimeClientRowDto
{
    public int ClientId { get; init; }
    public string ClientPib { get; init; } = "";
    public DateOnly FirstCaseDate { get; init; }
}
