using System.Security.Claims;

namespace CaseFlow.PAGES.Extensions;

public static class DetectiveIdentity
{
    public static string? FromUser(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name;
}
