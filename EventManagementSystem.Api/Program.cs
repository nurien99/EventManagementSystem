using EventManagementSystem.Api.Data;
using EventManagementSystem.Api.Models;
using EventManagementSystem.Api.Services;
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core.Configuration;
using EventManagementSystem.Core.DTOs;
using FluentEmail.Core;
using FluentEmail.Razor;
using FluentEmail.Smtp;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// --- CORS Configuration ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        policy.WithOrigins("https://localhost:7120", "http://localhost:5234", "https://localhost:7203", "https://localhost:7155", "http://localhost:5252")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- JWT Authentication Setup ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? ""));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = secretKey,
        ClockSkew = TimeSpan.FromMinutes(5)
    };
});

// Database connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ✅ ADD DATA PROTECTION
builder.Services.AddDataProtection()
    .SetApplicationName("EventManagementSystem");

// Configuration
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<SiteSettings>(builder.Configuration.GetSection("SiteSettings"));
builder.Services.Configure<FrontendSettings>(builder.Configuration.GetSection("FrontendSettings"));

// ✅ ENHANCED FLUENT EMAIL CONFIGURATION
var emailSettings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>();
if (emailSettings != null && emailSettings.EnableEmailSending)
{
    builder.Services
        .AddFluentEmail(emailSettings.SenderEmail, emailSettings.SenderName)
        .AddRazorRenderer()
        .AddSmtpSender(emailSettings.SmtpServer, emailSettings.Port, emailSettings.Username, emailSettings.Password);

    Console.WriteLine($"📧 Email configured: {emailSettings.SenderEmail} via {emailSettings.SmtpServer}:{emailSettings.Port}");
}
else
{
    Console.WriteLine("⚠️ Email sending is disabled or not configured properly");
}

// ✅ HANGFIRE CONFIGURATION - CRITICAL FIX
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

// ✅ HANGFIRE SERVER WITH PROPER CONFIGURATION
builder.Services.AddHangfireServer(options =>
{
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
    options.WorkerCount = Math.Max(Environment.ProcessorCount, 2);
    options.Queues = new[] { "default", "email" }; // Add email queue
});

// ✅ REGISTER SERVICES IN CORRECT ORDER
builder.Services.AddScoped<IQRCodeService, QRCodeService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IEmailTemplateService, RazorEmailTemplateService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IVenueService, VenueService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IEventAssistantService, EventAssistantService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<BackgroundEmailProcessor>();

builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Add("/EmailTemplates/{0}.cshtml");
        options.ViewLocationFormats.Add("/EmailTemplates/{1}/{0}.cshtml");
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Event Management System API",
        Version = "v1",
        Description = "A comprehensive API for managing events, registrations, and tickets"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// ✅ CRITICAL FIX - SET SERVICE PROVIDER AFTER APP IS BUILT
ServiceProviderAccessor.ServiceProvider = app.Services;

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

// ✅ STATIC FILE SERVING FOR UPLOADS
app.UseStaticFiles(); // Default wwwroot folder
app.UseCors("AllowBlazorApp");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// ✅ HANGFIRE DASHBOARD WITH BETTER CONFIG
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    DisplayStorageConnectionString = false,
    DashboardTitle = "Event Management System - Background Jobs",
    StatsPollingInterval = 2000 // Refresh every 2 seconds
});

app.MapControllers();

// ✅ STARTUP DIAGNOSTICS
Console.WriteLine("🚀 Event Management System API is starting...");
Console.WriteLine($"📊 Hangfire Dashboard: https://localhost:7203/hangfire");
Console.WriteLine($"📖 API Documentation: https://localhost:7203/swagger");

// ✅ TEST EMAIL SERVICE INITIALIZATION
try
{
    using var scope = app.Services.CreateScope();
    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
    Console.WriteLine("✅ Email service initialized successfully");

    // Test service provider accessor
    var testScope = ServiceProviderAccessor.ServiceProvider?.CreateScope();
    if (testScope != null)
    {
        Console.WriteLine("✅ ServiceProviderAccessor working correctly");
        testScope.Dispose();
    }
    else
    {
        Console.WriteLine("❌ ServiceProviderAccessor not working");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Email service initialization failed: {ex.Message}");
}

app.Run();


public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // Use GetHttpContext() consistently - this is the correct way for Hangfire
        var httpContext = context.GetHttpContext();

        // Allow in development environment
        var environment = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (environment.IsDevelopment())
            return true;

        // In production, require authentication
        return httpContext.User.Identity.IsAuthenticated &&
               httpContext.User.IsInRole("Admin");
    }
}
