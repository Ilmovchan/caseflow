using System.Security.Claims;

namespace CaseFlow.PAGES.Extensions;

/// <summary>Detective login uses PostgreSQL role name in <see cref="ClaimTypes.Email"/> (see <c>AuthService</c>).</summary>
public static class DetectiveIdentity
{
    public static string? FromUser(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name;
}
