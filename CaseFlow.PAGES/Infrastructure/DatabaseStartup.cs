using CaseFlow.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CaseFlow.PAGES.Infrastructure;

public static partial class DatabaseStartup
{
    public static async Task InitializeAsync(WebApplication app)
    {
        var applyRoleGrantsOnStartup = app.Configuration.GetValue("Database:ApplyRoleGrantsOnStartup", defaultValue: true);
        var applyStartupMaintenance = app.Configuration.GetValue("Database:ApplyStartupMaintenance", defaultValue: false);

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DetectiveAgencyDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInit");

            try
            {
                if (!await AgencyTablesExistAsync(db))
                    await db.Database.EnsureCreatedAsync();
            }
            catch (PostgresException ex) when (ex.SqlState == "3D000")
            {
                await db.Database.EnsureCreatedAsync();
            }

            try
            {
                if (!await AgencyTablesExistAsync(db))
                {
                    logger.LogInformation(
                        "Skipping detective/admin role grants: core tables are missing (EnsureCreated did not create schema; check connection and permissions).");
                }
                else
                {
                    if (!applyRoleGrantsOnStartup)
                    {
                        logger.LogDebug(
                            "Skipping detective/admin GRANT on startup (Database:ApplyRoleGrantsOnStartup=false). Configure privileges in PostgreSQL manually or via migration scripts.");
                    }
                    else
                    {
                        try
                        {
                            await ApplyDetectiveRoleGrantsAsync(db);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex,
                                "Could not apply detective role grants (DB user may need CREATEROLE/superuser). Detective logins may fail until grants are applied manually.");
                        }

                        try
                        {
                            await ApplyAdminRoleGrantsAsync(db);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex,
                                "Could not apply admin role grants (DB user may need superuser or table owner). Admin panel may return permission denied until grants are applied manually.");
                        }
                    }

                    try
                    {
                        if (await IsCaseflowRowLevelSecurityInstalledAsync(db))
                        {
                            logger.LogDebug("Row-level security is already installed; skipping reapply.");
                        }
                        else
                        {
                            await ApplyRowLevelSecurityAsync(db);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "Could not apply row level security (needs table owner / superuser).");
                    }

                    try
                    {
                        if (await IsListingADatabaseObjectsInstalledAsync(db))
                            logger.LogDebug("Об'єкти БД з лістингів А вже встановлено; пропуск.");
                        else
                            await ApplyListingADatabaseObjectsAsync(db);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "Не вдалося створити функції/подання/процедури з лістингів А (потрібні права власника таблиць або суперкористувач).");
                    }

                    if (applyStartupMaintenance)
                    {
                        try
                        {
                            await EnsureCaseDetectiveIdNullableAsync(db);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex,
                                "Could not alter case.detective_id to nullable (needs table owner). Unlinking a detective from a case may fail.");
                        }

                        try
                        {
                            await ApplyForceRowLevelSecurityWorkflowTablesAsync(db);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex,
                                "Could not FORCE ROW LEVEL SECURITY on workflow tables (needs table owner). Table owners may bypass RLS.");
                        }

                        try
                        {
                            await ApplyDetectiveAvailabilityTriggerAsync(db);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex,
                                "Could not apply check_detective_availability trigger (needs table owner / superuser).");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Could not verify schema for DB role grants.");
            }
        }
    }

private static async Task<bool> AgencyTablesExistAsync(DetectiveAgencyDbContext dbContext)
{
    await dbContext.Database.OpenConnectionAsync();
    try
    {
        await using var cmd = dbContext.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'case')
            """;
        var scalar = await cmd.ExecuteScalarAsync();
        return scalar is bool b && b;
    }
    finally
    {
        await dbContext.Database.CloseConnectionAsync();
    }
}

private static async Task<bool> IsCaseflowRowLevelSecurityInstalledAsync(DetectiveAgencyDbContext dbContext)
{
    await dbContext.Database.OpenConnectionAsync();
    try
    {
        await using var cmd = dbContext.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = """
            SELECT EXISTS (
                SELECT 1 FROM pg_policies
                WHERE schemaname = 'public'
                  AND tablename = 'case'
                  AND policyname = 'case_rls_admin_all'
            )
            AND EXISTS (
                SELECT 1 FROM pg_proc p
                JOIN pg_namespace n ON n.oid = p.pronamespace
                WHERE n.nspname = 'public' AND p.proname = 'cf_current_detective_id'
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

private static async Task ApplyDetectiveRoleGrantsAsync(DetectiveAgencyDbContext dbContext)
{
    await dbContext.Database.ExecuteSqlRawAsync(
        """
        DO $grant$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'detective') THEN
                CREATE ROLE detective NOLOGIN;
            END IF;

            GRANT USAGE ON SCHEMA public TO detective;

            -- Cases: detectives read and may update fields via DetectiveService (no create/delete of cases).
            GRANT SELECT, UPDATE ON TABLE "case" TO detective;
            GRANT SELECT ON TABLE case_type TO detective;
            GRANT SELECT ON TABLE client TO detective;
            GRANT SELECT ON TABLE detective TO detective;

            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE evidence TO detective;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE case_evidence TO detective;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE suspect TO detective;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE case_suspect TO detective;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE expense TO detective;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE report TO detective;

            GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO detective;

            GRANT USAGE ON TYPE case_status TO detective;
            GRANT USAGE ON TYPE detective_status TO detective;
            GRANT USAGE ON TYPE evidence_type TO detective;
            GRANT USAGE ON TYPE approval_status TO detective;

            ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO detective;
            ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO detective;
        END
        $grant$;
        """);
}

private static async Task ApplyAdminRoleGrantsAsync(DetectiveAgencyDbContext dbContext)
{
    await dbContext.Database.ExecuteSqlRawAsync(
        """
        DO $grant$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'admin') THEN
                CREATE ROLE admin NOLOGIN;
            END IF;

            GRANT USAGE ON SCHEMA public TO admin;

            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE "case" TO admin;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE case_type TO admin;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE client TO admin;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE detective TO admin;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE evidence TO admin;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE case_evidence TO admin;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE suspect TO admin;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE case_suspect TO admin;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE expense TO admin;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE report TO admin;

            GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO admin;

            GRANT USAGE ON TYPE case_status TO admin;
            GRANT USAGE ON TYPE detective_status TO admin;
            GRANT USAGE ON TYPE evidence_type TO admin;
            GRANT USAGE ON TYPE approval_status TO admin;

            ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO admin;
            ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO admin;
        END
        $grant$;
        """);
}

private static async Task ApplyRowLevelSecurityAsync(DetectiveAgencyDbContext dbContext)
{
    await dbContext.Database.ExecuteSqlRawAsync(
        """
        BEGIN;

        DROP POLICY IF EXISTS case_rls_admin_all ON "case";
        DROP POLICY IF EXISTS case_rls_detective_select ON "case";
        DROP POLICY IF EXISTS case_rls_detective_update ON "case";
        DROP POLICY IF EXISTS client_rls_admin_all ON client;
        DROP POLICY IF EXISTS client_rls_detective_select ON client;
        DROP POLICY IF EXISTS case_type_rls_admin_all ON case_type;
        DROP POLICY IF EXISTS case_type_rls_detective_select ON case_type;
        DROP POLICY IF EXISTS detective_rls_admin_all ON detective;
        DROP POLICY IF EXISTS detective_rls_detective_select ON detective;
        DROP POLICY IF EXISTS evidence_rls_admin_all ON evidence;
        DROP POLICY IF EXISTS evidence_rls_admin_select ON evidence;
        DROP POLICY IF EXISTS evidence_rls_admin_insert ON evidence;
        DROP POLICY IF EXISTS evidence_rls_admin_update ON evidence;
        DROP POLICY IF EXISTS evidence_rls_admin_delete ON evidence;
        DROP POLICY IF EXISTS evidence_rls_detective_select ON evidence;
        DROP POLICY IF EXISTS evidence_rls_detective_insert ON evidence;
        DROP POLICY IF EXISTS evidence_rls_detective_update ON evidence;
        DROP POLICY IF EXISTS evidence_rls_detective_delete ON evidence;
        DROP POLICY IF EXISTS case_evidence_rls_admin_all ON case_evidence;
        DROP POLICY IF EXISTS case_evidence_rls_admin_select ON case_evidence;
        DROP POLICY IF EXISTS case_evidence_rls_admin_insert ON case_evidence;
        DROP POLICY IF EXISTS case_evidence_rls_admin_update ON case_evidence;
        DROP POLICY IF EXISTS case_evidence_rls_admin_delete ON case_evidence;
        DROP POLICY IF EXISTS case_evidence_rls_detective_select ON case_evidence;
        DROP POLICY IF EXISTS case_evidence_rls_detective_insert ON case_evidence;
        DROP POLICY IF EXISTS case_evidence_rls_detective_delete ON case_evidence;
        DROP POLICY IF EXISTS suspect_rls_admin_all ON suspect;
        DROP POLICY IF EXISTS suspect_rls_admin_select ON suspect;
        DROP POLICY IF EXISTS suspect_rls_admin_insert ON suspect;
        DROP POLICY IF EXISTS suspect_rls_admin_update ON suspect;
        DROP POLICY IF EXISTS suspect_rls_admin_delete ON suspect;
        DROP POLICY IF EXISTS suspect_rls_detective_select ON suspect;
        DROP POLICY IF EXISTS suspect_rls_detective_insert ON suspect;
        DROP POLICY IF EXISTS suspect_rls_detective_update ON suspect;
        DROP POLICY IF EXISTS suspect_rls_detective_delete ON suspect;
        DROP POLICY IF EXISTS case_suspect_rls_admin_all ON case_suspect;
        DROP POLICY IF EXISTS case_suspect_rls_admin_select ON case_suspect;
        DROP POLICY IF EXISTS case_suspect_rls_admin_insert ON case_suspect;
        DROP POLICY IF EXISTS case_suspect_rls_admin_update ON case_suspect;
        DROP POLICY IF EXISTS case_suspect_rls_admin_delete ON case_suspect;
        DROP POLICY IF EXISTS case_suspect_rls_detective_select ON case_suspect;
        DROP POLICY IF EXISTS case_suspect_rls_detective_insert ON case_suspect;
        DROP POLICY IF EXISTS case_suspect_rls_detective_delete ON case_suspect;
        DROP POLICY IF EXISTS expense_rls_admin_all ON expense;
        DROP POLICY IF EXISTS expense_rls_admin_select ON expense;
        DROP POLICY IF EXISTS expense_rls_admin_insert ON expense;
        DROP POLICY IF EXISTS expense_rls_admin_update ON expense;
        DROP POLICY IF EXISTS expense_rls_admin_delete ON expense;
        DROP POLICY IF EXISTS expense_rls_detective_select ON expense;
        DROP POLICY IF EXISTS expense_rls_detective_insert ON expense;
        DROP POLICY IF EXISTS expense_rls_detective_update ON expense;
        DROP POLICY IF EXISTS expense_rls_detective_delete ON expense;
        DROP POLICY IF EXISTS report_rls_admin_all ON report;
        DROP POLICY IF EXISTS report_rls_admin_select ON report;
        DROP POLICY IF EXISTS report_rls_admin_insert ON report;
        DROP POLICY IF EXISTS report_rls_admin_update ON report;
        DROP POLICY IF EXISTS report_rls_admin_delete ON report;
        DROP POLICY IF EXISTS report_rls_detective_select ON report;
        DROP POLICY IF EXISTS report_rls_detective_insert ON report;
        DROP POLICY IF EXISTS report_rls_detective_update ON report;
        DROP POLICY IF EXISTS report_rls_detective_delete ON report;

        DROP FUNCTION IF EXISTS cf_case_evidence_link_evidence_allowed(integer);
        DROP FUNCTION IF EXISTS cf_case_suspect_link_suspect_allowed(integer);
        DROP FUNCTION IF EXISTS cf_current_detective_id();

        CREATE FUNCTION cf_current_detective_id() RETURNS integer
        LANGUAGE sql
        STABLE
        SET search_path = public
        AS $fn$
            SELECT d.id
            FROM detective d
            WHERE d.postgres_login IS NOT NULL
              AND lower(d.postgres_login) = lower(current_user::text)
            LIMIT 1;
        $fn$;

        GRANT EXECUTE ON FUNCTION cf_current_detective_id() TO admin;
        GRANT EXECUTE ON FUNCTION cf_current_detective_id() TO detective;

        -- Avoid infinite recursion: read evidence with row_security off. Only admin-approved rows may be linked to a case.
        CREATE OR REPLACE FUNCTION cf_case_evidence_link_evidence_allowed(evidence_row_id integer)
        RETURNS boolean
        LANGUAGE sql
        STABLE
        SECURITY DEFINER
        SET row_security = off
        SET search_path = public
        AS $cef$
            SELECT EXISTS (
                SELECT 1
                FROM evidence e
                WHERE e.id = evidence_row_id
                  AND e.approval_status = 'Схвалено'::approval_status
            );
        $cef$;

        GRANT EXECUTE ON FUNCTION cf_case_evidence_link_evidence_allowed(integer) TO admin;
        GRANT EXECUTE ON FUNCTION cf_case_evidence_link_evidence_allowed(integer) TO detective;

        -- Same pattern: suspect SELECT references case_suspect; only approved suspects may be linked.
        CREATE OR REPLACE FUNCTION cf_case_suspect_link_suspect_allowed(suspect_row_id integer)
        RETURNS boolean
        LANGUAGE sql
        STABLE
        SECURITY DEFINER
        SET row_security = off
        SET search_path = public
        AS $css$
            SELECT EXISTS (
                SELECT 1
                FROM suspect s
                WHERE s.id = suspect_row_id
                  AND s.approval_status = 'Схвалено'::approval_status
            );
        $css$;

        GRANT EXECUTE ON FUNCTION cf_case_suspect_link_suspect_allowed(integer) TO admin;
        GRANT EXECUTE ON FUNCTION cf_case_suspect_link_suspect_allowed(integer) TO detective;

        ALTER TABLE "case" ENABLE ROW LEVEL SECURITY;
        ALTER TABLE client ENABLE ROW LEVEL SECURITY;
        ALTER TABLE case_type ENABLE ROW LEVEL SECURITY;
        ALTER TABLE detective ENABLE ROW LEVEL SECURITY;
        ALTER TABLE evidence ENABLE ROW LEVEL SECURITY;
        ALTER TABLE case_evidence ENABLE ROW LEVEL SECURITY;
        ALTER TABLE suspect ENABLE ROW LEVEL SECURITY;
        ALTER TABLE case_suspect ENABLE ROW LEVEL SECURITY;
        ALTER TABLE expense ENABLE ROW LEVEL SECURITY;
        ALTER TABLE report ENABLE ROW LEVEL SECURITY;

        -- Table owners otherwise bypass RLS; superusers still bypass unless session is non-superuser.
        ALTER TABLE evidence FORCE ROW LEVEL SECURITY;
        ALTER TABLE case_evidence FORCE ROW LEVEL SECURITY;
        ALTER TABLE suspect FORCE ROW LEVEL SECURITY;
        ALTER TABLE case_suspect FORCE ROW LEVEL SECURITY;
        ALTER TABLE expense FORCE ROW LEVEL SECURITY;
        ALTER TABLE report FORCE ROW LEVEL SECURITY;

        CREATE POLICY case_rls_admin_all ON "case"
            FOR ALL TO PUBLIC
            USING (pg_has_role(current_user, 'admin', 'member'))
            WITH CHECK (pg_has_role(current_user, 'admin', 'member'));

        CREATE POLICY case_rls_detective_select ON "case"
            FOR SELECT TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND detective_id = cf_current_detective_id()
            );

        CREATE POLICY case_rls_detective_update ON "case"
            FOR UPDATE TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND detective_id = cf_current_detective_id()
            )
            WITH CHECK (
                pg_has_role(current_user, 'detective', 'member')
                AND detective_id = cf_current_detective_id()
            );

        CREATE POLICY client_rls_admin_all ON client
            FOR ALL TO PUBLIC
            USING (pg_has_role(current_user, 'admin', 'member'))
            WITH CHECK (pg_has_role(current_user, 'admin', 'member'));

        CREATE POLICY client_rls_detective_select ON client
            FOR SELECT TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1
                    FROM "case" c
                    WHERE c.client_id = client.id
                      AND c.detective_id = cf_current_detective_id()
                )
            );

        CREATE POLICY case_type_rls_admin_all ON case_type
            FOR ALL TO PUBLIC
            USING (pg_has_role(current_user, 'admin', 'member'))
            WITH CHECK (pg_has_role(current_user, 'admin', 'member'));

        CREATE POLICY case_type_rls_detective_select ON case_type
            FOR SELECT TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1
                    FROM "case" c
                    WHERE c.case_type_id = case_type.id
                      AND c.detective_id = cf_current_detective_id()
                )
            );

        CREATE POLICY detective_rls_admin_all ON detective
            FOR ALL TO PUBLIC
            USING (pg_has_role(current_user, 'admin', 'member'))
            WITH CHECK (pg_has_role(current_user, 'admin', 'member'));

        CREATE POLICY detective_rls_detective_select ON detective
            FOR SELECT TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND postgres_login IS NOT NULL
                AND lower(postgres_login) = lower(current_user::text)
            );

        CREATE POLICY evidence_rls_admin_all ON evidence
            FOR ALL TO PUBLIC
            USING (pg_has_role(current_user, 'admin', 'member'))
            WITH CHECK (pg_has_role(current_user, 'admin', 'member'));

        CREATE POLICY evidence_rls_detective_select ON evidence
            FOR SELECT TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND (
                    created_by_detective_id = cf_current_detective_id()
                    OR approval_status = 'Схвалено'::approval_status
                    OR EXISTS (
                        SELECT 1
                        FROM case_evidence ce
                        JOIN "case" c ON c.id = ce.case_id
                        WHERE ce.evidence_id = evidence.id
                          AND c.detective_id = cf_current_detective_id()
                    )
                )
            );

        CREATE POLICY evidence_rls_detective_insert ON evidence
            FOR INSERT TO PUBLIC
            WITH CHECK (
                pg_has_role(current_user, 'detective', 'member')
                AND created_by_detective_id = cf_current_detective_id()
            );

        CREATE POLICY evidence_rls_detective_update ON evidence
            FOR UPDATE TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND (
                    created_by_detective_id = cf_current_detective_id()
                    OR (
                        created_by_detective_id IS NULL
                        AND EXISTS (
                            SELECT 1
                            FROM case_evidence ce
                            JOIN "case" c ON c.id = ce.case_id
                            WHERE ce.evidence_id = evidence.id
                              AND c.detective_id = cf_current_detective_id()
                        )
                    )
                )
                AND approval_status IN (
                    'Чернетка'::approval_status,
                    'Відхилено'::approval_status
                )
            )
            WITH CHECK (
                pg_has_role(current_user, 'detective', 'member')
                AND (
                    created_by_detective_id = cf_current_detective_id()
                    OR (
                        created_by_detective_id IS NULL
                        AND EXISTS (
                            SELECT 1
                            FROM case_evidence ce
                            JOIN "case" c ON c.id = ce.case_id
                            WHERE ce.evidence_id = evidence.id
                              AND c.detective_id = cf_current_detective_id()
                        )
                    )
                )
                AND approval_status IN (
                    'Чернетка'::approval_status,
                    'Відхилено'::approval_status,
                    'Очікує'::approval_status
                )
            );

        CREATE POLICY evidence_rls_detective_delete ON evidence
            FOR DELETE TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND (
                    created_by_detective_id = cf_current_detective_id()
                    OR (
                        created_by_detective_id IS NULL
                        AND EXISTS (
                            SELECT 1
                            FROM case_evidence ce
                            JOIN "case" c ON c.id = ce.case_id
                            WHERE ce.evidence_id = evidence.id
                              AND c.detective_id = cf_current_detective_id()
                        )
                    )
                )
                AND approval_status IN (
                    'Чернетка'::approval_status,
                    'Відхилено'::approval_status
                )
            );

        CREATE POLICY case_evidence_rls_admin_all ON case_evidence
            FOR ALL TO PUBLIC
            USING (pg_has_role(current_user, 'admin', 'member'))
            WITH CHECK (pg_has_role(current_user, 'admin', 'member'));

        CREATE POLICY case_evidence_rls_detective_select ON case_evidence
            FOR SELECT TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = case_evidence.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
            );

        CREATE POLICY case_evidence_rls_detective_insert ON case_evidence
            FOR INSERT TO PUBLIC
            WITH CHECK (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = case_evidence.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
                AND cf_case_evidence_link_evidence_allowed(case_evidence.evidence_id)
            );

        CREATE POLICY case_evidence_rls_detective_delete ON case_evidence
            FOR DELETE TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = case_evidence.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
            );

        CREATE POLICY suspect_rls_admin_all ON suspect
            FOR ALL TO PUBLIC
            USING (pg_has_role(current_user, 'admin', 'member'))
            WITH CHECK (pg_has_role(current_user, 'admin', 'member'));

        CREATE POLICY suspect_rls_detective_select ON suspect
            FOR SELECT TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND (
                    created_by_detective_id = cf_current_detective_id()
                    OR approval_status = 'Схвалено'::approval_status
                    OR EXISTS (
                        SELECT 1
                        FROM case_suspect cs
                        JOIN "case" c ON c.id = cs.case_id
                        WHERE cs.suspect_id = suspect.id
                          AND c.detective_id = cf_current_detective_id()
                    )
                )
            );

        CREATE POLICY suspect_rls_detective_insert ON suspect
            FOR INSERT TO PUBLIC
            WITH CHECK (
                pg_has_role(current_user, 'detective', 'member')
                AND created_by_detective_id = cf_current_detective_id()
            );

        CREATE POLICY suspect_rls_detective_update ON suspect
            FOR UPDATE TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND (
                    created_by_detective_id = cf_current_detective_id()
                    OR (
                        created_by_detective_id IS NULL
                        AND EXISTS (
                            SELECT 1
                            FROM case_suspect cs
                            JOIN "case" c ON c.id = cs.case_id
                            WHERE cs.suspect_id = suspect.id
                              AND c.detective_id = cf_current_detective_id()
                        )
                    )
                )
                AND approval_status IN (
                    'Чернетка'::approval_status,
                    'Відхилено'::approval_status
                )
            )
            WITH CHECK (
                pg_has_role(current_user, 'detective', 'member')
                AND (
                    created_by_detective_id = cf_current_detective_id()
                    OR (
                        created_by_detective_id IS NULL
                        AND EXISTS (
                            SELECT 1
                            FROM case_suspect cs
                            JOIN "case" c ON c.id = cs.case_id
                            WHERE cs.suspect_id = suspect.id
                              AND c.detective_id = cf_current_detective_id()
                        )
                    )
                )
                AND approval_status IN (
                    'Чернетка'::approval_status,
                    'Відхилено'::approval_status,
                    'Очікує'::approval_status
                )
            );

        CREATE POLICY suspect_rls_detective_delete ON suspect
            FOR DELETE TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND (
                    created_by_detective_id = cf_current_detective_id()
                    OR (
                        created_by_detective_id IS NULL
                        AND EXISTS (
                            SELECT 1
                            FROM case_suspect cs
                            JOIN "case" c ON c.id = cs.case_id
                            WHERE cs.suspect_id = suspect.id
                              AND c.detective_id = cf_current_detective_id()
                        )
                    )
                )
                AND approval_status IN (
                    'Чернетка'::approval_status,
                    'Відхилено'::approval_status
                )
            );

        CREATE POLICY case_suspect_rls_admin_all ON case_suspect
            FOR ALL TO PUBLIC
            USING (pg_has_role(current_user, 'admin', 'member'))
            WITH CHECK (pg_has_role(current_user, 'admin', 'member'));

        CREATE POLICY case_suspect_rls_detective_select ON case_suspect
            FOR SELECT TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = case_suspect.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
            );

        CREATE POLICY case_suspect_rls_detective_insert ON case_suspect
            FOR INSERT TO PUBLIC
            WITH CHECK (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = case_suspect.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
                AND cf_case_suspect_link_suspect_allowed(case_suspect.suspect_id)
            );

        CREATE POLICY case_suspect_rls_detective_delete ON case_suspect
            FOR DELETE TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = case_suspect.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
            );

        CREATE POLICY expense_rls_admin_all ON expense
            FOR ALL TO PUBLIC
            USING (pg_has_role(current_user, 'admin', 'member'))
            WITH CHECK (pg_has_role(current_user, 'admin', 'member'));

        CREATE POLICY expense_rls_detective_select ON expense
            FOR SELECT TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = expense.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
            );

        CREATE POLICY expense_rls_detective_insert ON expense
            FOR INSERT TO PUBLIC
            WITH CHECK (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = expense.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
                AND created_by_detective_id = cf_current_detective_id()
            );

        CREATE POLICY expense_rls_detective_update ON expense
            FOR UPDATE TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = expense.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
                AND created_by_detective_id = cf_current_detective_id()
                AND approval_status IN (
                    'Чернетка'::approval_status,
                    'Відхилено'::approval_status
                )
            )
            WITH CHECK (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = expense.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
                AND created_by_detective_id = cf_current_detective_id()
                AND approval_status IN (
                    'Чернетка'::approval_status,
                    'Відхилено'::approval_status,
                    'Очікує'::approval_status
                )
            );

        CREATE POLICY expense_rls_detective_delete ON expense
            FOR DELETE TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = expense.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
                AND created_by_detective_id = cf_current_detective_id()
                AND approval_status IN (
                    'Чернетка'::approval_status,
                    'Відхилено'::approval_status
                )
            );

        CREATE POLICY report_rls_admin_all ON report
            FOR ALL TO PUBLIC
            USING (pg_has_role(current_user, 'admin', 'member'))
            WITH CHECK (pg_has_role(current_user, 'admin', 'member'));

        CREATE POLICY report_rls_detective_select ON report
            FOR SELECT TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = report.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
            );

        CREATE POLICY report_rls_detective_insert ON report
            FOR INSERT TO PUBLIC
            WITH CHECK (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = report.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
                AND created_by_detective_id = cf_current_detective_id()
            );

        CREATE POLICY report_rls_detective_update ON report
            FOR UPDATE TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = report.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
                AND created_by_detective_id = cf_current_detective_id()
                AND approval_status IN (
                    'Чернетка'::approval_status,
                    'Відхилено'::approval_status
                )
            )
            WITH CHECK (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = report.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
                AND created_by_detective_id = cf_current_detective_id()
                AND approval_status IN (
                    'Чернетка'::approval_status,
                    'Відхилено'::approval_status,
                    'Очікує'::approval_status
                )
            );

        CREATE POLICY report_rls_detective_delete ON report
            FOR DELETE TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND EXISTS (
                    SELECT 1 FROM "case" c
                    WHERE c.id = report.case_id
                      AND c.detective_id = cf_current_detective_id()
                )
                AND created_by_detective_id = cf_current_detective_id()
                AND approval_status IN (
                    'Чернетка'::approval_status,
                    'Відхилено'::approval_status
                )
            );

        COMMIT;
        """);
}

private static async Task ApplyForceRowLevelSecurityWorkflowTablesAsync(DetectiveAgencyDbContext dbContext)
{
    await dbContext.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE evidence FORCE ROW LEVEL SECURITY;
        ALTER TABLE case_evidence FORCE ROW LEVEL SECURITY;
        ALTER TABLE suspect FORCE ROW LEVEL SECURITY;
        ALTER TABLE case_suspect FORCE ROW LEVEL SECURITY;
        ALTER TABLE expense FORCE ROW LEVEL SECURITY;
        ALTER TABLE report FORCE ROW LEVEL SECURITY;
        """);
}

private static async Task EnsureCaseDetectiveIdNullableAsync(DetectiveAgencyDbContext dbContext)
{
    await dbContext.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE "case" ALTER COLUMN detective_id DROP NOT NULL;
        """);
}

private static async Task ApplyDetectiveAvailabilityTriggerAsync(DetectiveAgencyDbContext dbContext)
{
    await dbContext.Database.ExecuteSqlRawAsync(
        """
        CREATE OR REPLACE FUNCTION check_detective_availability()
        RETURNS TRIGGER
        LANGUAGE plpgsql
        SET search_path = public
        AS $fn$
        DECLARE
            active_count integer;
            max_active_cases constant integer := 5;
        BEGIN
            IF NEW.detective_id IS NULL THEN
                RETURN NEW;
            END IF;

            SELECT COUNT(*)::integer INTO active_count
            FROM "case" c
            WHERE c.detective_id = NEW.detective_id
              AND c.status <> 'Закрито'::case_status
              AND (TG_OP = 'INSERT' OR c.id <> NEW.id);

            IF active_count >= max_active_cases THEN
                RAISE EXCEPTION 'У детектива вже максимум активних справ (%). Звільніть справу (відв’яжіть детектива), закрийте або передайте іншому детективу перед новим призначенням.',
                    max_active_cases
                    USING ERRCODE = '23514';
            END IF;

            RETURN NEW;
        END;
        $fn$;

        GRANT EXECUTE ON FUNCTION check_detective_availability() TO admin;
        GRANT EXECUTE ON FUNCTION check_detective_availability() TO detective;

        DO $apply$
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM pg_trigger t
                JOIN pg_class c ON c.oid = t.tgrelid
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE t.tgname = 'tr_case_check_detective_availability'
                  AND c.relname = 'case'
                  AND n.nspname = 'public'
                  AND NOT t.tgisinternal
            ) THEN
                CREATE TRIGGER tr_case_check_detective_availability
                BEFORE INSERT OR UPDATE OF detective_id ON "case"
                FOR EACH ROW
                EXECUTE PROCEDURE check_detective_availability();
            END IF;
        END;
        $apply$;
        """);
}

}
