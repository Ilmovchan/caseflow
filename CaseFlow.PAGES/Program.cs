using CaseFlow.API.Extensions;
using CaseFlow.BLL.MappingProfiles;
using CaseFlow.BLL.Services;
using CaseFlow.PAGES.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Подключение базы и AutoMapper
builder.Services
    .ConfigureDatabase(builder.Configuration)
    .AddAutoMapper(typeof(CaseFlowMappingProfile).Assembly);

// Регистрация сервисов
builder.Services
    .AddScoped<AdminService>()
    .AddScoped<DetectiveService>();

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
app.UseAuthorization();

// Razor Pages
app.MapRazorPages();

app.Run();