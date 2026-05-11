using System.Security.Claims;
using CaseFlow.PAGES;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Http;

namespace CaseFlow.PAGES.Infrastructure;

public sealed class DetectiveRlsExecutionContext(IHttpContextAccessor httpAccessor)
    : IDetectiveRlsExecutionContext
{
    public bool RowLevelSecurityEnforcesDetectiveCaseScope
    {
        get
        {
            var http = httpAccessor.HttpContext;
            if (http?.User?.IsInRole("Detective") != true)
                return false;
            return !string.IsNullOrEmpty(http.Session?.GetString(PgSessionKeys.Password));
        }
    }
}
