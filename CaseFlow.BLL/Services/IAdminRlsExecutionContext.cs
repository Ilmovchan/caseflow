namespace CaseFlow.BLL.Services;

/// <summary>Чи активні політики RLS для адміна (пароль PostgreSQL у веб-сесії).</summary>
public interface IAdminRlsExecutionContext
{
    bool RowLevelSecurityAdminPoliciesActive { get; }
}
