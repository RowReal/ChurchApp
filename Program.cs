using ChurchApp.Components;
using ChurchApp.Data;
using ChurchApp.Models;
using ChurchApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// for troubleshooting
if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(options =>
    {
        options.DetailedErrors = true;
    });
}


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
builder.Services.AddScoped<ImageOptimizationService>();
builder.Services.AddScoped<ChurchUpdateService>();
builder.Services.AddScoped<ChurchNoticeService>();
builder.Services.AddScoped<VerseOfTheDayService>();
builder.Services.AddScoped<PrayerFocusService>();
builder.Services.AddScoped<PrivilegeService>();

builder.Services.AddCascadingAuthenticationState();

builder.Services.Configure<EmailConfiguration>(
    builder.Configuration.GetSection("EmailConfiguration"));

builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<ExcuseService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HtmlCleanService>();
// Approval services
builder.Services.AddScoped<FinancialRequestService>();
builder.Services.AddScoped<ApprovalAttachmentService>();
builder.Services.AddScoped<ApprovalNotificationService>();
builder.Services.AddScoped<ApprovalRoutingService>();
builder.Services.AddScoped<ApprovalWorkflowService>();
builder.Services.AddScoped<ApprovalQueryService>();
builder.Services.AddScoped<ApprovalDecisionService>();
builder.Services.AddScoped<ApprovalSubmissionService>();
builder.Services.AddScoped<LeaveRequestService>();

//Service Record/Offering Services
builder.Services.AddScoped<ChurchOfferingAccessService>();

builder.Services.AddScoped<ChurchOfferingService>();

builder.Services.AddScoped<ApprovalRequestService>();
builder.Services.AddScoped<ServiceRecordAccessService>();
builder.Services.AddScoped<VehicleRecordService>();
var app = builder.Build();

// APPLY EF CORE MIGRATIONS AUTOMATICALLY
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    Console.WriteLine("Applying database migrations...");

    db.Database.Migrate();

    Console.WriteLine("Database migrations completed.");
}
// Seed approval request types and workflows AFTER migration
using (var scope = app.Services.CreateScope())
{
    var approvalRequestService = scope.ServiceProvider.GetRequiredService<ApprovalRequestService>();

    Console.WriteLine("Seeding approval request types and workflows...");

    await approvalRequestService.SeedApprovalRequestTypesAndWorkflowsAsync();

    Console.WriteLine("Approval request types and workflows seeded.");
}
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


// ==============================
// ✅ FIXED UPLOAD PATH (IMPORTANT)
// ==============================
string uploadsPath;

if (app.Environment.IsDevelopment())
{
    uploadsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
}
else
{
    uploadsPath = "/var/data/uploads";
}

if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseHttpsRedirection();

app.UseStaticFiles();

// Serve uploads from SAME folder we write to
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

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