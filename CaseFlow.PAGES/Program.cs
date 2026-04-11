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

// Create/update schema from EF migrations when the database is empty or behind (e.g. tables dropped).
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DetectiveAgencyDbContext>();
    await db.Database.MigrateAsync();

    try
    {
        await ApplyDetectiveRoleGrantsAsync(db);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseInit");
        logger.LogWarning(ex,
            "Could not apply detective role grants (DB user may need CREATEROLE/superuser). Detective logins may fail until grants are applied manually.");
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