using System.ComponentModel.DataAnnotations.Schema;

namespace CaseFlow.BLL.Dto.AdminSpecial;

public sealed class DetectiveRankingViewRowDto
{
    [Column("detective_id")]
    public int DetectiveId { get; set; }

    [Column("detective_name")]
    public string DetectiveName { get; set; } = "";

    [Column("closed_case_percentage")]
    public decimal? ClosedCasePercentage { get; set; }

    [Column("overdue_cases_count")]
    public long OverdueCasesCount { get; set; }

    [Column("average_case_cost")]
    public decimal? AverageCaseCost { get; set; }

    [Column("total_evidence_count")]
    public long TotalEvidenceCount { get; set; }

    [Column("total_suspects_count")]
    public long TotalSuspectsCount { get; set; }
}

public sealed class FirstTimeClientViewRowDto
{
    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; } = "";

    [Column("last_name")]
    public string LastName { get; set; } = "";

    [Column("first_case_date")]
    public DateOnly FirstCaseDate { get; set; }
}
