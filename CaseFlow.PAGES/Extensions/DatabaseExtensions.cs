using CaseFlow.DAL.Data;
using CaseFlow.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CaseFlow.PAGES.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection ConfigureDatabase(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connString = configuration.GetConnectionString("DetectiveAgencyDb");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
        dataSourceBuilder.EnableUnmappedTypes();

        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<DetectiveAgencyDbContext>(options =>
            options.UseNpgsql(dataSource, o =>
            {
                o.MapEnum<CaseStatus>("case_status");
                o.MapEnum<DetectiveStatus>("detective_status");
                o.MapEnum<EvidenceType>("evidence_type");
                o.MapEnum<ApprovalStatus>("approval_status");
            }));

        return services;
    }
}