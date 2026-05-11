namespace CaseFlow.BLL.Services;

/// <summary>Чи RLS у PostgreSQL обмежує дані детектива (логін PG + пароль у сесії).</summary>
public interface IDetectiveRlsExecutionContext
{
    bool RowLevelSecurityEnforcesDetectiveCaseScope { get; }
}
