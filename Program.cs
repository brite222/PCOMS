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


AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);


// =========================
// Database (PostgreSQL) - WITH RETRY LOGIC
// =========================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            // Enable retry on transient failures (PostgreSQL requires errorCodesToAdd parameter)
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null); // null means use default error codes

            // Increase command timeout for slow connections
            npgsqlOptions.CommandTimeout(60);
        }
    ));
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
// Apply Migrations (WITH ERROR HANDLING)
// =========================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Starting database migration...");
        var db = services.GetRequiredService<ApplicationDbContext>();

        // Test connection first
        var canConnect = await db.Database.CanConnectAsync();
        logger.LogInformation("Database connection test: {CanConnect}", canConnect);

        if (canConnect)
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("✅ Database migration completed successfully");
        }
        else
        {
            logger.LogError("❌ Cannot connect to database. Check connection string.");
            logger.LogError("Connection string (masked): {ConnectionString}",
                MaskConnectionString(builder.Configuration.GetConnectionString("DefaultConnection")));
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error during database migration");
        logger.LogError("Connection string (masked): {ConnectionString}",
            MaskConnectionString(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Don't crash - let the app start so we can see errors in logs
        // throw; // Uncomment to crash on DB errors
    }
}

// =========================
// Create Roles if Missing
// =========================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = { "Admin", "ProjectManager", "Developer", "Client" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("✅ Created role: {Role}", role);
            }
        }

        logger.LogInformation("✅ Role check completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error creating roles");
    }
}

// =========================
// Seed Roles & Admin
// =========================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        await RoleSeeder.SeedRolesAsync(services);
        await UserSeeder.SeedAdminAsync(services);
        logger.LogInformation("✅ Seeding completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error during seeding");
    }
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

// Final log before starting
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("🚀 PCOMS Application started successfully");

app.Run();

// =========================
// HELPER METHOD
// =========================
static string? MaskConnectionString(string? connectionString)
{
    if (string.IsNullOrEmpty(connectionString))
        return "NULL";

    // Hide password in logs
    var masked = connectionString;
    if (masked.Contains("Password=", StringComparison.OrdinalIgnoreCase))
    {
        var parts = masked.Split(';');
        masked = string.Join(";", parts.Select(p =>
            p.Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase)
                ? "Password=***HIDDEN***"
                : p));
    }

    return masked;
}