namespace CaseFlow.PAGES;

/// <summary>Session storage for PostgreSQL password after login (used to build EF connection as the authenticated DB user).</summary>
public static class PgSessionKeys
{
    public const string Password = "CaseFlowPgPassword";
}
