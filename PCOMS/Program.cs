using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PCOMS.Data;
using PCOMS.Data.Seed;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// =========================
// MVC
// =========================
builder.Services.AddControllersWithViews();

// =========================
// Database (SQLite — EB safe)
// =========================
var dbDir = Path.Combine(AppContext.BaseDirectory, "app_data");
Directory.CreateDirectory(dbDir);

var dbPath = Path.Combine(dbDir, "PCOMS.db");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}")
);

// =========================
// Identity
// =========================
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// =========================
// Application Services
// =========================
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectAssignmentService, ProjectAssignmentService>();
builder.Services.AddScoped<ITimeEntryService, TimeEntryService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<InvoiceNumberGenerator>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<InvoicePdfService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<ICommunicationService, CommunicationService>();
builder.Services.AddScoped<ITimeTrackingService, TimeTrackingService>();
builder.Services.AddScoped<IProjectTemplateService, ProjectTemplateService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<SubmissionService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<ResourceService>();
builder.Services.Configure<PCOMS.Application.Settings.EmailSettings>(
    builder.Configuration.GetSection("Email"));

// =========================
// Build app
// =========================
var app = builder.Build();

// =========================
// Ensure DB + Migrations (AWS fix)
// =========================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

// =========================
// Create roles if missing
// =========================
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Admin", "ProjectManager", "Developer", "Client" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// =========================
// Ensure upload folder exists
// =========================
var env = app.Services.GetRequiredService<IWebHostEnvironment>();
Directory.CreateDirectory(
    Path.Combine(env.WebRootPath, "uploads", "documents"));

// =========================
// Seed Roles & Admin
// =========================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await RoleSeeder.SeedRolesAsync(services);
    await UserSeeder.SeedAdminAsync(services);
}

// =========================
// Middleware
// =========================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// =========================
// Routing
// =========================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Clients}/{action=Index}/{id?}");

app.Run();
