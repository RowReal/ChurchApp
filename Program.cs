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
    options.Username = Environment.GetEnvironmentVariable("EMAIL_USERNAME");
    options.Password = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
    options.SmtpServer = Environment.GetEnvironmentVariable("EMAIL_SMTP");
    options.Port = int.Parse(Environment.GetEnvironmentVariable("EMAIL_PORT") ?? "587");
    options.EnableSsl = bool.Parse(Environment.GetEnvironmentVariable("EMAIL_ENABLESSL") ?? "true");
    options.FromEmail = Environment.GetEnvironmentVariable("EMAIL_FROMEMAIL");
    options.FromName = Environment.GetEnvironmentVariable("EMAIL_FROMNAME");
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


//using (var scope = app.Services.CreateScope())
//{
//    var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

//    await emailService.SendEmailAsync(new EmailMessage
//    {
//        ToEmail = "row_real@yahoo.com",
//        ToName = "Test",
//        Subject = "ChurchApp Gmail Test",
//        Body = "<h3>Email is working successfully 🎉</h3>",
//        IsHtml = true
//    });
//}

app.Run();
