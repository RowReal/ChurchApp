using ChurchApp.Components;
using ChurchApp.Data;
using ChurchApp.Models;
using ChurchApp.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();



// 1?? Determine dbPath and optional seed (your existing code)
string dataFolder;
if (builder.Environment.IsDevelopment())
    dataFolder = Path.Combine(builder.Environment.ContentRootPath, "Data");
else
    dataFolder = "/var/data";

if (!Directory.Exists(dataFolder))
    Directory.CreateDirectory(dataFolder);

var dbPath = Path.Combine(dataFolder, "churchapp.db");

Console.WriteLine($"Using SQLite DB at: {dbPath}");

// Seed from wwwroot/seed/churchapp.db if missing
if (!builder.Environment.IsDevelopment())
{
    var seedDbPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot/seed/churchapp.db");
    if (!File.Exists(dbPath) && File.Exists(seedDbPath))
        File.Copy(seedDbPath, dbPath);
}

// 2?? Register DbContext FIRST
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// 3?? Register services that depend on AppDbContext
builder.Services.AddScoped<WorkerService>(provider =>
{
    var context = provider.GetRequiredService<AppDbContext>();
    var auditService = provider.GetRequiredService<AuditService>();
    var authService = provider.GetRequiredService<AuthService>();
    return new WorkerService(context, auditService, authService);
});


builder.Services.AddScoped<DataSeederService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<WorkforceService>();
builder.Services.AddScoped<OfferingService>();
builder.Services.AddScoped<ReportingService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<AccountabilityService>();
builder.Services.AddScoped<RecordNominationService>();
builder.Services.AddScoped<ServiceService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<ServiceNoteService>();
builder.Services.AddScoped<GuestService>();

builder.Services.AddCascadingAuthenticationState();

builder.Services.Configure<EmailConfiguration>(options =>
{
    options.SmtpServer = builder.Configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
    options.Port = int.Parse(builder.Configuration["Email:Port"] ?? "587");
    options.Username = builder.Configuration["Email:Username"] ?? "";
    options.Password = builder.Configuration["Email:Password"] ?? "";
    options.EnableSsl = bool.Parse(builder.Configuration["Email:EnableSsl"] ?? "true");
    options.FromEmail = builder.Configuration["Email:FromEmail"] ?? "noreply@yourchurch.com";
    options.FromName = builder.Configuration["Email:FromName"] ?? "BCC ServiceHub";
});

builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<ExcuseService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Create uploads directory
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseAntiforgery();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeederService>();
    await seeder.SeedDataAsync();
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
