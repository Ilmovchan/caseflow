using System.Data.Common;
using System.Security.Claims;
using CaseFlow.PAGES;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CaseFlow.PAGES.Infrastructure;

public sealed class PgConnectionAuditInterceptor(
    IHttpContextAccessor httpContextAccessor,
    ILogger<PgConnectionAuditInterceptor> logger) : DbConnectionInterceptor
{
    public override InterceptionResult ConnectionOpening(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        LogConnectionOpening(connection);
        return base.ConnectionOpening(connection, eventData, result);
    }

    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        LogConnectionOpening(connection);
        return await base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken)
            .ConfigureAwait(false);
    }

    private void LogConnectionOpening(DbConnection connection)
    {
        if (connection is not NpgsqlConnection)
            return;

        string connStr;
        try
        {
            connStr = connection.ConnectionString;
        }
        catch
        {
            return;
        }

        if (string.IsNullOrEmpty(connStr))
            return;

        var csb = new NpgsqlConnectionStringBuilder(connStr);
        var pgUser = csb.Username ?? "(невідомо)";
        var database = csb.Database ?? "(невідомо)";
        var host = csb.Host ?? "(невідомо)";
        var applicationName = string.IsNullOrEmpty(csb.ApplicationName) ? "(немає)" : csb.ApplicationName;

        var http = httpContextAccessor.HttpContext;
        string? appLogin = null;
        string? appRole = null;
        var authenticated = http?.User?.Identity?.IsAuthenticated == true;
        if (authenticated)
        {
            appLogin = http!.User.Identity?.Name;
            appRole = http.User.FindFirstValue(ClaimTypes.Role);
        }

        var sessionPwd = http?.Session?.GetString(PgSessionKeys.Password);
        var usesDirectPgSession =
            authenticated
            && !string.IsNullOrEmpty(sessionPwd)
            && !string.IsNullOrEmpty(appLogin);

        logger.LogDebug(
            "Сесія PostgreSQL: хост={Host}, база={Database}, pg_user={PgUser}, application_name={ApplicationName}, " +
            "http_login={HttpLogin}, роль_застосунку={AppRole}, прямі_облікові_pg={DirectPg}",
            host,
            database,
            pgUser,
            applicationName,
            appLogin,
            appRole,
            usesDirectPgSession);
    }
}
