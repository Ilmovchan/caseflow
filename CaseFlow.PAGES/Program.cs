using CaseFlow.PAGES.Extensions;
using CaseFlow.BLL.MappingProfiles;
using CaseFlow.BLL.Services;
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

// Применение миграций и создание БД (без ручного EnsureCreated)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DetectiveAgencyDbContext>();
    await dbContext.Database.MigrateAsync();

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
END $$;
");
}

// Создаём default users только после создания БД
using (var scope = app.Services.CreateScope())
{
    var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
    await authService.CreateDefaultUsersAsync();
}

// Razor Pages
app.MapRazorPages();

// API Controllers
app.MapControllers();

app.Run();