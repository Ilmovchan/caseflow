using CaseFlow.PAGES.Extensions;
using CaseFlow.PAGES.Infrastructure;
using CaseFlow.BLL.MappingProfiles;
using CaseFlow.BLL.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .ConfigureDatabase(builder.Configuration)
    .ConfigureControllers()
    .AddAutoMapper(typeof(CaseFlowMappingProfile).Assembly);

builder.Services
    .AddScoped<IDetectiveRlsExecutionContext, DetectiveRlsExecutionContext>()
    .AddScoped<IAdminRlsExecutionContext, AdminRlsExecutionContext>()
    .AddScoped<AdminService>()
    .AddScoped<DetectiveService>()
    .AddScoped<AuthService>();

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

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddRazorPages();

var app = builder.Build();

await DatabaseStartup.InitializeAsync(app);

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
app.UseMiddleware<RequirePostgreSqlSessionMiddleware>();
app.UseAuthorization();

app.MapRazorPages();

app.MapControllers();

app.Run();