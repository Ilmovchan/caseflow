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

// Migrations: PostgreSQL "postgres" role (see ConnectionStrings:DetectiveAgencyDbMigration).
// Runtime EF: same PostgreSQL user as login (e.g. admin_test); password kept in session after sign-in.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StartupMigration");
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    try
    {
        var migrationConn = NpgsqlConnectionStringHelper.ApplyEnvironmentOverrides(
            configuration.GetConnectionString("DetectiveAgencyDbMigration")
            ?? configuration.GetConnectionString("DetectiveAgencyDb"));
        if (string.IsNullOrWhiteSpace(migrationConn))
        {
            logger.LogWarning("No migration connection string; skipping migrations.");
        }
        else
        {
            var optionsBuilder = new DbContextOptionsBuilder<DetectiveAgencyDbContext>();
            optionsBuilder.ConfigureDetectiveDbContextOptions(migrationConn);
            await using var dbContext = new DetectiveAgencyDbContext(optionsBuilder.Options);
            try
            {
                await dbContext.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                // Still run idempotent patch + sequence fixes (e.g. history out of sync with actual DB).
                logger.LogWarning(ex, "EF MigrateAsync failed; continuing with idempotent schema patch and sequence reseed.");
            }

            // Ensures detective.postgres_login + index exist even when migrations were skipped earlier.
            try
            {
                await ApplyDetectivePostgresLoginSchemaPatchAsync(dbContext);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Idempotent postgres_login schema patch failed (check DB permissions).");
            }

            // Detective logins use their own PostgreSQL user (inherits group role `detective`). Table owner is
            // typically the migration user, so detectives get 42501 until we grant DML + enum USAGE + sequences.
            try
            {
                await ApplyDetectiveRoleGrantsAsync(dbContext);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Idempotent GRANT for role detective failed (run app startup as a DB superuser once).");
            }

        // If the `detective.id` sequence is out of sync with existing rows, inserts may fail with
        // "duplicate key value violates unique constraint PK_detective". Reseed to MAX(id)+1.
        await dbContext.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF pg_get_serial_sequence('detective', 'id') IS NOT NULL THEN
        PERFORM setval(
            pg_get_serial_sequence('detective', 'id'),
            COALESCE((SELECT MAX(id) FROM detective), 0),
            true
        );
    END IF;
END $$;
");

        // Same issue can happen for `case.id` (PK_case).
        await dbContext.Database.ExecuteSqlRawAsync(@"
DO $$
DECLARE seq text;
BEGIN
    -- If the sequence is out of sync, new inserts can collide with existing PK values.
    seq := pg_get_serial_sequence('case', 'id');
    IF seq IS NOT NULL THEN
        PERFORM setval(
            seq::regclass,
            COALESCE((SELECT MAX(id) FROM ""case""), 0),
            true
        );
    END IF;
END $$;
");

        // Same for `case_type.id` (PK_case_type) — out-of-sync sequence causes duplicate key on insert.
        await dbContext.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF pg_get_serial_sequence('case_type', 'id') IS NOT NULL THEN
        PERFORM setval(
            pg_get_serial_sequence('case_type', 'id'),
            COALESCE((SELECT MAX(id) FROM case_type), 0),
            true
        );
    END IF;
END $$;
");

        // Reseed other identity PK sequences that are frequently inserted from UI forms.
        await dbContext.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF pg_get_serial_sequence('evidence', 'id') IS NOT NULL THEN
        PERFORM setval(
            pg_get_serial_sequence('evidence', 'id'),
            COALESCE((SELECT MAX(id) FROM evidence), 0),
            true
        );
    END IF;

    IF pg_get_serial_sequence('suspect', 'id') IS NOT NULL THEN
        PERFORM setval(
            pg_get_serial_sequence('suspect', 'id'),
            COALESCE((SELECT MAX(id) FROM suspect), 0),
            true
        );
    END IF;

    IF pg_get_serial_sequence('report', 'id') IS NOT NULL THEN
        PERFORM setval(
            pg_get_serial_sequence('report', 'id'),
            COALESCE((SELECT MAX(id) FROM report), 0),
            true
        );
    END IF;

    IF pg_get_serial_sequence('expense', 'id') IS NOT NULL THEN
        PERFORM setval(
            pg_get_serial_sequence('expense', 'id'),
            COALESCE((SELECT MAX(id) FROM expense), 0),
            true
        );
    END IF;

    IF pg_get_serial_sequence('client', 'id') IS NOT NULL THEN
        PERFORM setval(
            pg_get_serial_sequence('client', 'id'),
            COALESCE((SELECT MAX(id) FROM client), 0),
            true
        );
    END IF;
END $$;
");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration/sequence reseed failed. Fix the connection or apply migrations manually; admin pages will error if schema is out of date.");
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
/// own Npgsql login. Row-level filtering (e.g. only assigned cases) stays in application code (<see cref="CaseFlow.BLL.Services.DetectiveService"/>).
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

// Razor Pages
app.MapRazorPages();

// API Controllers
app.MapControllers();

app.Run();