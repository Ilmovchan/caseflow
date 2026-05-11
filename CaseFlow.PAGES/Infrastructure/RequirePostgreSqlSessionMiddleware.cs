using CaseFlow.PAGES;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Extensions;

namespace CaseFlow.PAGES.Infrastructure;

public sealed class RequirePostgreSqlSessionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (AllowsBypass(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        await context.Session.LoadAsync();
        if (!string.IsNullOrEmpty(context.Session.GetString(PgSessionKeys.Password)))
        {
            await next(context);
            return;
        }

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var returnUrl = context.Request.GetEncodedPathAndQuery();
        var login = "/Auth/Login";
        if (!string.IsNullOrEmpty(returnUrl)
            && returnUrl != "/"
            && !returnUrl.StartsWith("/Auth/Login", StringComparison.OrdinalIgnoreCase))
        {
            login += "?returnUrl=" + Uri.EscapeDataString(returnUrl);
        }

        context.Response.Redirect(login);
    }

    private static bool AllowsBypass(PathString path)
    {
        if (path.StartsWithSegments("/Auth/Login", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.StartsWithSegments("/Auth/Logout", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }
}
