using CaseFlow.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.PAGES.Infrastructure;

public static partial class DatabaseStartup
{
    private static async Task<bool> IsListingADatabaseObjectsInstalledAsync(DetectiveAgencyDbContext dbContext)
    {
        await dbContext.Database.OpenConnectionAsync();
        try
        {
            await using var cmd = dbContext.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = """
                SELECT EXISTS (
                    SELECT 1 FROM pg_proc p
                    JOIN pg_namespace n ON n.oid = p.pronamespace
                    WHERE n.nspname = 'public' AND p.proname = 'get_average_case_cost'
                )
                AND EXISTS (
                    SELECT 1 FROM pg_views WHERE schemaname = 'public' AND viewname = 'detective_ranking'
                );
                """;
            var scalar = await cmd.ExecuteScalarAsync();
            return scalar is bool b && b;
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private static async Task ApplyListingADatabaseObjectsAsync(DetectiveAgencyDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE OR REPLACE FUNCTION get_average_case_cost(_detective_id INTEGER)
            RETURNS TABLE (
                detective_name VARCHAR,
                average_case_cost NUMERIC(10, 2)
            )
            LANGUAGE plpgsql
            SET search_path = public
            AS $fn$
            BEGIN
                RETURN QUERY
                SELECT
                    d.first_name || ' ' || d.last_name AS detective_name,
                    ROUND(AVG(cd.total_case_cost), 2) AS average_case_cost
                FROM (
                    SELECT
                        ca.id AS case_id,
                        ca.detective_id,
                        ct.price + COALESCE(SUM(e.amount), 0) AS total_case_cost
                    FROM public."case" ca
                    JOIN public.case_type ct ON ca.case_type_id = ct.id
                    LEFT JOIN public.expense e ON ca.id = e.case_id
                    WHERE ca.detective_id = _detective_id
                    GROUP BY ca.id, ca.detective_id, ct.price
                ) AS cd
                JOIN public.detective d ON cd.detective_id = d.id
                WHERE d.id = _detective_id
                GROUP BY d.first_name, d.last_name;
            END;
            $fn$;

            DROP VIEW IF EXISTS public.detective_ranking;
            CREATE VIEW public.detective_ranking AS
            SELECT
                d.id AS detective_id,
                (d.first_name::text || ' '::text) || d.last_name::text AS detective_name,
                ROUND(
                    COUNT(*) FILTER (
                        WHERE ca.status = 'Закрито'::case_status
                          AND ca.start_date >= DATE_TRUNC('year', CURRENT_DATE::timestamp with time zone)
                    )::numeric * 100.0
                    / NULLIF(
                        COUNT(*) FILTER (
                            WHERE ca.start_date >= DATE_TRUNC('year', CURRENT_DATE::timestamp with time zone)
                        ),
                        0
                    )::numeric,
                    2
                ) AS closed_case_percentage,
                COUNT(*) FILTER (
                    WHERE ca.close_date IS NOT NULL
                      AND ca.deadline_date < ca.close_date
                      AND ca.start_date >= DATE_TRUNC('year', CURRENT_DATE::timestamp with time zone)
                ) AS overdue_cases_count,
                (SELECT average_case_cost FROM get_average_case_cost(d.id) LIMIT 1) AS average_case_cost,
                COUNT(DISTINCT ce.evidence_id) FILTER (
                    WHERE ca.start_date >= DATE_TRUNC('year', CURRENT_DATE::timestamp with time zone)
                ) AS total_evidence_count,
                COUNT(DISTINCT cs.suspect_id) FILTER (
                    WHERE ca.start_date >= DATE_TRUNC('year', CURRENT_DATE::timestamp with time zone)
                ) AS total_suspects_count
            FROM public.detective d
            LEFT JOIN public."case" ca ON d.id = ca.detective_id
            LEFT JOIN public.case_evidence ce ON ca.id = ce.case_id
            LEFT JOIN public.case_suspect cs ON ca.id = cs.case_id
            GROUP BY d.id, d.first_name, d.last_name;

            DROP VIEW IF EXISTS public.first_time_clients;
            CREATE VIEW public.first_time_clients AS
            SELECT
                c.id AS client_id,
                c.first_name,
                c.last_name,
                MIN(ca.start_date) AS first_case_date
            FROM public.client c
            JOIN public."case" ca ON c.id = ca.client_id
            GROUP BY c.id, c.first_name, c.last_name
            HAVING MIN(ca.start_date) >= DATE_TRUNC('year', CURRENT_DATE::timestamp with time zone);

            GRANT EXECUTE ON FUNCTION get_average_case_cost(integer) TO admin;
            GRANT EXECUTE ON FUNCTION get_average_case_cost(integer) TO detective;
            GRANT SELECT ON public.detective_ranking TO admin;
            GRANT SELECT ON public.first_time_clients TO admin;
            """);

    }
}
