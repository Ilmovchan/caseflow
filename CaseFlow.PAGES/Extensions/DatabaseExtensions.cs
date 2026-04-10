using System.Security.Claims;
using CaseFlow.PAGES;
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

        var dataSource = dataSourceBuilder.Build();

        options.UseNpgsql(dataSource, o =>
        {
            o.MapEnum<CaseStatus>("case_status");
            o.MapEnum<DetectiveStatus>("detective_status");
            o.MapEnum<EvidenceType>("evidence_type");
            o.MapEnum<ApprovalStatus>("approval_status");
        });
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
        var baseConn = configuration.GetConnectionString("DetectiveAgencyDb")
            ?? throw new InvalidOperationException("ConnectionStrings:DetectiveAgencyDb is required.");

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