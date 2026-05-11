using CaseFlow.PAGES.Extensions;
using CaseFlow.BLL.MappingProfiles;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Configuration;
using CaseFlow.DAL.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Подключение базы и AutoMapper
builder.Services
    .ConfigureDatabase(builder.Configuration)
    .ConfigureControllers()
    .AddAutoMapper(typeof(CaseFlowMappingProfile).Assembly);

// Регистрация сервисов
builder.Services
    .AddScoped<AdminService>()
    .AddScoped<DetectiveService>()
    .AddScoped<AuthService>();

// Authentication and Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("DetectiveOnly", policy => policy.RequireRole("Detective"));
    options.AddPolicy("AdminOrDetective", policy => policy.RequireRole("Admin", "Detective"));
});

// Session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    // Align with auth cookie so PostgreSQL credentials in session stay available for EF for the signed-in period.
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

// Create schema from the current model if the database is empty (no migrations; model changes require drop/recreate).
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DetectiveAgencyDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInit");

    await db.Database.EnsureCreatedAsync();

    try
    {
        await ApplyDetectivePostgresLoginSchemaPatchAsync(db);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex,
            "Could not apply legacy detective postgres_login / Users migration patch.");
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

            try
            {
                await ApplyRowLevelSecurityAsync(db);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Could not apply row level security (needs table owner / superuser).");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex,
            "Could not verify schema for DB role grants.");
    }
}

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

static async Task<bool> AgencyTablesExistAsync(DetectiveAgencyDbContext dbContext)
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

static async Task ApplyDetectivePostgresLoginSchemaPatchAsync(DetectiveAgencyDbContext dbContext)
{
    await dbContext.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE detective ADD COLUMN IF NOT EXISTS postgres_login character varying(100);
        """);

    await dbContext.Database.ExecuteSqlRawAsync(
        """
        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1 FROM pg_catalog.pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = 'public' AND c.relname = 'Users'
            ) THEN
                UPDATE detective d
                SET postgres_login = u."Username"
                FROM "Users" u
                WHERE u."Email" = d.email AND u."Role" = 'Detective';
            END IF;
        END $$;
        """);

    await dbContext.Database.ExecuteSqlRawAsync(
        """
        DROP TABLE IF EXISTS "Users";
        """);

    await dbContext.Database.ExecuteSqlRawAsync(
        """
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_detective_postgres_login" ON detective (postgres_login);
        """);
}

/// <summary>
/// Grants the <c>detective</c> group role access to agency tables so EF works when detectives connect with their
/// own Npgsql login. Fine-grained row access for detectives is enforced in PostgreSQL via
/// <see cref="ApplyRowLevelSecurityAsync"/> (RLS); <see cref="CaseFlow.BLL.Services.DetectiveService"/> keeps the same rules at the app layer.
/// </summary>
static async Task ApplyDetectiveRoleGrantsAsync(DetectiveAgencyDbContext dbContext)
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

/// <summary>
/// Grants the <c>admin</c> group role full DML on agency tables. Admins sign in with their own PostgreSQL login
/// (<see cref="CaseFlow.BLL.Services.AuthService"/> checks <c>pg_has_role(current_user, 'admin', 'member')</c>), so
/// that login must inherit privileges from <c>admin</c> — same pattern as <c>detective</c>.
/// </summary>
static async Task ApplyAdminRoleGrantsAsync(DetectiveAgencyDbContext dbContext)
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

/// <summary>
/// Enables PostgreSQL RLS so that sessions using <c>detective</c> membership only see rows tied to
/// <c>detective.postgres_login = current_user</c>. Admins (<c>pg_has_role(..., 'admin', 'member')</c>) bypass row filters via policy.
/// The migration connection (often table owner or superuser) is not restricted by RLS.
/// </summary>
static async Task ApplyRowLevelSecurityAsync(DetectiveAgencyDbContext dbContext)
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
        DROP POLICY IF EXISTS evidence_rls_detective_select ON evidence;
        DROP POLICY IF EXISTS evidence_rls_detective_insert ON evidence;
        DROP POLICY IF EXISTS evidence_rls_detective_update ON evidence;
        DROP POLICY IF EXISTS evidence_rls_detective_delete ON evidence;
        DROP POLICY IF EXISTS case_evidence_rls_admin_all ON case_evidence;
        DROP POLICY IF EXISTS case_evidence_rls_detective_select ON case_evidence;
        DROP POLICY IF EXISTS case_evidence_rls_detective_insert ON case_evidence;
        DROP POLICY IF EXISTS case_evidence_rls_detective_delete ON case_evidence;
        DROP POLICY IF EXISTS suspect_rls_admin_all ON suspect;
        DROP POLICY IF EXISTS suspect_rls_detective_select ON suspect;
        DROP POLICY IF EXISTS suspect_rls_detective_insert ON suspect;
        DROP POLICY IF EXISTS suspect_rls_detective_update ON suspect;
        DROP POLICY IF EXISTS suspect_rls_detective_delete ON suspect;
        DROP POLICY IF EXISTS case_suspect_rls_admin_all ON case_suspect;
        DROP POLICY IF EXISTS case_suspect_rls_detective_select ON case_suspect;
        DROP POLICY IF EXISTS case_suspect_rls_detective_insert ON case_suspect;
        DROP POLICY IF EXISTS case_suspect_rls_detective_delete ON case_suspect;
        DROP POLICY IF EXISTS expense_rls_admin_all ON expense;
        DROP POLICY IF EXISTS expense_rls_detective_select ON expense;
        DROP POLICY IF EXISTS expense_rls_detective_insert ON expense;
        DROP POLICY IF EXISTS expense_rls_detective_update ON expense;
        DROP POLICY IF EXISTS expense_rls_detective_delete ON expense;
        DROP POLICY IF EXISTS report_rls_admin_all ON report;
        DROP POLICY IF EXISTS report_rls_detective_select ON report;
        DROP POLICY IF EXISTS report_rls_detective_insert ON report;
        DROP POLICY IF EXISTS report_rls_detective_update ON report;
        DROP POLICY IF EXISTS report_rls_detective_delete ON report;

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
                AND created_by_detective_id = cf_current_detective_id()
                AND approval_status IN (
                    'Чернетка'::approval_status,
                    'Відхилено'::approval_status
                )
            )
            WITH CHECK (
                pg_has_role(current_user, 'detective', 'member')
                AND created_by_detective_id = cf_current_detective_id()
            );

        CREATE POLICY evidence_rls_detective_delete ON evidence
            FOR DELETE TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND created_by_detective_id = cf_current_detective_id()
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
                AND (
                    EXISTS (
                        SELECT 1 FROM evidence e
                        WHERE e.id = case_evidence.evidence_id
                          AND e.created_by_detective_id = cf_current_detective_id()
                    )
                    OR EXISTS (
                        SELECT 1 FROM evidence e
                        WHERE e.id = case_evidence.evidence_id
                          AND e.approval_status = 'Схвалено'::approval_status
                    )
                )
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
                AND created_by_detective_id = cf_current_detective_id()
                AND approval_status IN (
                    'Чернетка'::approval_status,
                    'Відхилено'::approval_status
                )
            )
            WITH CHECK (
                pg_has_role(current_user, 'detective', 'member')
                AND created_by_detective_id = cf_current_detective_id()
            );

        CREATE POLICY suspect_rls_detective_delete ON suspect
            FOR DELETE TO PUBLIC
            USING (
                pg_has_role(current_user, 'detective', 'member')
                AND created_by_detective_id = cf_current_detective_id()
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
                AND (
                    EXISTS (
                        SELECT 1 FROM suspect s
                        WHERE s.id = case_suspect.suspect_id
                          AND s.created_by_detective_id = cf_current_detective_id()
                    )
                    OR EXISTS (
                        SELECT 1 FROM suspect s
                        WHERE s.id = case_suspect.suspect_id
                          AND s.approval_status = 'Схвалено'::approval_status
                    )
                )
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

// Razor Pages
app.MapRazorPages();

// API Controllers
app.MapControllers();

app.Run();