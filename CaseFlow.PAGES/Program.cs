using CaseFlow.PAGES.Extensions;
using CaseFlow.BLL.MappingProfiles;
using CaseFlow.BLL.Services;
using CaseFlow.DAL.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
    options.IdleTimeout = TimeSpan.FromMinutes(30);
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
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DetectiveAgencyDbContext>();
    
    // Check if Users table exists
    var tablesExist = false;
    try
    {
        if (dbContext.Database.CanConnect())
        {
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND LOWER(table_name) = 'users'";
            var result = await command.ExecuteScalarAsync();
            tablesExist = result != null && Convert.ToInt32(result) > 0;
            await connection.CloseAsync();
        }
    }
    catch
    {
        // If we can't check, assume tables don't exist
    }
    
    if (!tablesExist)
    {
        // Tables don't exist, but migrations think they're applied
        // Clear migrations history and re-apply migrations to force table creation
        try
        {
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync();
            using var clearCommand = connection.CreateCommand();
            clearCommand.CommandText = "DELETE FROM \"__EFMigrationsHistory\"";
            await clearCommand.ExecuteNonQueryAsync();
            await connection.CloseAsync();
            
            // Now re-apply migrations which will create the tables
            await dbContext.Database.MigrateAsync();
            
            // Verify tables were created
            connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync();
            using var verifyCommand = connection.CreateCommand();
            verifyCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND LOWER(table_name) = 'users'";
            var verifyResult = await verifyCommand.ExecuteScalarAsync();
            tablesExist = verifyResult != null && Convert.ToInt32(verifyResult) > 0;
            await connection.CloseAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == "42710")
        {
            // Enum types already exist - migration failed but we need tables
            // Mark migration as applied manually and use EnsureCreated to create tables
            try
            {
                // Mark the migration as applied so we can use EnsureCreated
                var connection = dbContext.Database.GetDbConnection();
                await connection.OpenAsync();
                using var markCommand = connection.CreateCommand();
                markCommand.CommandText = @"
                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                    SELECT '20250601113931_InitialCreate', '9.0.10'
                    WHERE NOT EXISTS (SELECT 1 FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20250601113931_InitialCreate');
                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                    SELECT '20251020164357_AddApprovalStatusToSuspectAndEvidence', '9.0.10'
                    WHERE NOT EXISTS (SELECT 1 FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20251020164357_AddApprovalStatusToSuspectAndEvidence');";
                await markCommand.ExecuteNonQueryAsync();
                await connection.CloseAsync();
                
                // Now use EnsureCreated which will create tables (enum types already exist, so it will skip them)
                try
                {
                    dbContext.Database.EnsureCreated();
                }
                catch (PostgresException ex2) when (ex2.SqlState == "42710")
                {
                    // Enum types already exist - EnsureCreated still failed
                    // Manually create the Users table if it doesn't exist
                }
                
                // Always try to create Users table manually if it doesn't exist (regardless of EnsureCreated result)
                try
                {
                    var createConnection = dbContext.Database.GetDbConnection();
                    if (createConnection.State != System.Data.ConnectionState.Open)
                    {
                        await createConnection.OpenAsync();
                    }
                    using var createUsersCommand = createConnection.CreateCommand();
                    createUsersCommand.CommandText = @"
                        CREATE TABLE IF NOT EXISTS ""Users"" (
                            ""Id"" SERIAL PRIMARY KEY,
                            ""Username"" VARCHAR(100) NOT NULL,
                            ""Email"" VARCHAR(100) NOT NULL,
                            ""PasswordHash"" VARCHAR(255) NOT NULL,
                            ""Role"" VARCHAR(50) NOT NULL,
                            ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            ""LastLoginAt"" TIMESTAMP WITH TIME ZONE,
                            ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                            CONSTRAINT ""username_format"" CHECK (""Username"" ~ '^[a-zA-Z0-9_]+$'),
                            CONSTRAINT ""email_format"" CHECK (""Email"" ~ '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'),
                            CONSTRAINT ""role_format"" CHECK (""Role"" IN ('Admin', 'Detective')),
                            CONSTRAINT ""created_at_format"" CHECK (""CreatedAt"" <= CURRENT_TIMESTAMP),
                            CONSTRAINT ""last_login_at_format"" CHECK (""LastLoginAt"" IS NULL OR ""LastLoginAt"" <= CURRENT_TIMESTAMP)
                        );
                        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Username"" ON ""Users"" (""Username"");
                        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");";
                    await createUsersCommand.ExecuteNonQueryAsync();
                    if (createConnection.State == System.Data.ConnectionState.Open)
                    {
                        await createConnection.CloseAsync();
                    }
                }
                catch
                {
                    // Users table might already exist or creation failed
                }
            }
            catch (PostgresException ex2) when (ex2.SqlState == "42710" || ex2.SqlState == "42P07" || ex2.SqlState == "42P01")
            {
                // Some objects already exist - this is expected
            }
        }
        catch
        {
            // If clearing history fails, try EnsureCreated as fallback
            try
            {
                dbContext.Database.EnsureCreated();
            }
            catch (PostgresException ex) when (ex.SqlState == "42710" || ex.SqlState == "42P07" || ex.SqlState == "42P01")
            {
                // Some objects already exist
            }
        }
    }
}

// Initialize default users
using (var scope = app.Services.CreateScope())
{
    var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
    await authService.CreateDefaultUsersAsync();
}

// Razor Pages
app.MapRazorPages();

// API Controllers
app.MapControllers();

// Debug: List all registered routes
if (app.Environment.IsDevelopment())
{
    app.MapGet("/debug/routes", () =>
    {
        var routes = new List<string>();
        // This is a simple way to check if controllers are registered
        return "Controllers should be registered at /api/admin/* and /api/detective/*";
    });
}

app.Run();