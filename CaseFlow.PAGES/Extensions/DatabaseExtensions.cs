using System.Security.Claims;
using CaseFlow.PAGES;
using CaseFlow.DAL.Configuration;
using CaseFlow.DAL.Data;
using CaseFlow.DAL.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CaseFlow.PAGES.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Npgsql + enum mapping shared by DI and startup migration (may use different connection strings).
    /// </summary>
    public static void ConfigureDetectiveDbContextOptions(
        this DbContextOptionsBuilder options, string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableUnmappedTypes();
        // Qualified names: avoids "more than one PostgreSQL type was found" when a duplicate type exists in another schema.
        dataSourceBuilder.MapEnum<CaseStatus>("public.case_status");
        dataSourceBuilder.MapEnum<DetectiveStatus>("public.detective_status");
        dataSourceBuilder.MapEnum<EvidenceType>("public.evidence_type");
        dataSourceBuilder.MapEnum<ApprovalStatus>("public.approval_status");

        var dataSource = dataSourceBuilder.Build();

        options.UseNpgsql(dataSource);
    }

    public static IServiceCollection ConfigureDatabase(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddDbContext<DetectiveAgencyDbContext>((sp, options) =>
        {
            var connectionString = ResolveRuntimeConnectionString(sp, configuration);
            options.ConfigureDetectiveDbContextOptions(connectionString);
        });

        return services;
    }

    /// <summary>
    /// After login, EF uses the same PostgreSQL user as the form (e.g. admin_test) via session-stored password.
    /// Otherwise falls back to ConnectionStrings:DetectiveAgencyDb (e.g. design-time / tools).
    /// </summary>
    private static string ResolveRuntimeConnectionString(IServiceProvider sp, IConfiguration configuration)
    {
        var baseConn = NpgsqlConnectionStringHelper.ApplyEnvironmentOverrides(
            configuration.GetConnectionString("DetectiveAgencyDb"));
        if (string.IsNullOrWhiteSpace(baseConn))
            throw new InvalidOperationException("ConnectionStrings:DetectiveAgencyDb is required.");

        var httpAccessor = sp.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userName = httpContext.User.FindFirstValue(ClaimTypes.Name);
            var pgPassword = httpContext.Session.GetString(PgSessionKeys.Password);
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(pgPassword))
            {
                var csb = new NpgsqlConnectionStringBuilder(baseConn)
                {
                    Username = userName,
                    Password = pgPassword
                };
                return csb.ConnectionString;
            }
        }

        return baseConn;
    }
}