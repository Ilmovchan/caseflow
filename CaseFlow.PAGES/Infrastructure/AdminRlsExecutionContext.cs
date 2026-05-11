using CaseFlow.BLL.Services;
using CaseFlow.PAGES;
using Microsoft.AspNetCore.Http;

namespace CaseFlow.PAGES.Infrastructure;

public sealed class AdminRlsExecutionContext(IHttpContextAccessor httpAccessor)
    : IAdminRlsExecutionContext
{
    public bool RowLevelSecurityAdminPoliciesActive =>
        httpAccessor.HttpContext?.User?.IsInRole("Admin") == true
        && !string.IsNullOrEmpty(httpAccessor.HttpContext.Session?.GetString(PgSessionKeys.Password));
}
