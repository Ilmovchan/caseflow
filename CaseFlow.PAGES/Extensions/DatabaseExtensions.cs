using System.Security.Claims;
using CaseFlow.PAGES;
using CaseFlow.PAGES.Infrastructure;
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
    private const int MaxPgApplicationNameLength = 63;

    public static void ConfigureDetectiveDbContextOptions(
        this DbContextOptionsBuilder options, string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableUnmappedTypes();
        dataSourceBuilder.MapEnum<CaseStatus>("public.case_status");
        dataSourceBuilder.MapEnum<DetectiveStatus>("public.detective_status");
        dataSourceBuilder.MapEnum<EvidenceType>("public.evidence_type");
        dataSourceBuilder.MapEnum<ApprovalStatus>("public.approval_status");

        var dataSource = dataSourceBuilder.Build();

        options.UseNpgsql(dataSource, npgsql =>
        {
            npgsql.MapEnum<CaseStatus>("case_status");
            npgsql.MapEnum<DetectiveStatus>("detective_status");
            npgsql.MapEnum<EvidenceType>("evidence_type");
            npgsql.MapEnum<ApprovalStatus>("approval_status");
        });
    }

    public static IServiceCollection ConfigureDatabase(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<PgConnectionAuditInterceptor>();
        services.AddDbContext<DetectiveAgencyDbContext>((sp, options) =>
        {
            var connectionString = ResolveRuntimeConnectionString(sp, configuration);
            options.ConfigureDetectiveDbContextOptions(connectionString);
            options.AddInterceptors(sp.GetRequiredService<PgConnectionAuditInterceptor>());
        });

        return services;
    }

    private static string ResolveRuntimeConnectionString(IServiceProvider sp, IConfiguration configuration)
    {
        var baseConn = NpgsqlConnectionStringHelper.ApplyEnvironmentOverrides(
            configuration.GetConnectionString("DetectiveAgencyDb"));
        if (string.IsNullOrWhiteSpace(baseConn))
            throw new InvalidOperationException("У конфігурації має бути ConnectionStrings:DetectiveAgencyDb.");

        var httpAccessor = sp.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpAccessor.HttpContext;

        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userName = httpContext.User.FindFirstValue(ClaimTypes.Name);
            var pgPassword = httpContext.Session.GetString(PgSessionKeys.Password);
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(pgPassword))
            {
                throw new InvalidOperationException(
                    "Для роботи з базою потрібен пароль PostgreSQL у сесії. Вийдіть із системи та увійдіть знову.");
            }

            var csb = new NpgsqlConnectionStringBuilder(baseConn)
            {
                Username = userName,
                Password = pgPassword
            };
            ApplyApplicationName(csb, httpContext, "сесія");
            return csb.ConnectionString;
        }

        var fallback = new NpgsqlConnectionStringBuilder(baseConn);
        ApplyApplicationName(fallback, httpContext, httpContext is null ? "немає-http" : "спільні");
        return fallback.ConnectionString;
    }

    private static void ApplyApplicationName(NpgsqlConnectionStringBuilder csb, HttpContext? httpContext, string scenario)
    {
        var name = BuildApplicationName(httpContext, scenario);
        if (!string.IsNullOrEmpty(name))
            csb.ApplicationName = name;
    }

    private static string BuildApplicationName(HttpContext? httpContext, string scenario)
    {
        string s;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var role = httpContext.User.FindFirstValue(ClaimTypes.Role) ?? "?";
            var login = httpContext.User.Identity?.Name ?? "?";
            s = $"caseflow:{scenario}:{role}:{login}";
        }
        else if (httpContext is not null)
            s = $"caseflow:{scenario}:анонім";
        else
            s = $"caseflow:{scenario}:немає-http";

        return s.Length <= MaxPgApplicationNameLength ? s : s[..MaxPgApplicationNameLength];
    }
}