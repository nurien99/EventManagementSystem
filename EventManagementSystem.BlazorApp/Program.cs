using EventManagementSystem.BlazorApp.Components;
using EventManagementSystem.BlazorApp.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Blazor Server options to prevent circuit disconnection issues
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    options.DisconnectedCircuitMaxRetained = 100;
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
});

// Add HttpClient for API calls
builder.Services.AddHttpClient("EventManagementAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:7203/"); // Your API URL
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ✅ Add Blazored LocalStorage (already had this)
builder.Services.AddBlazoredLocalStorage();

// ✅ ADD AUTHENTICATION SERVICES
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// ✅ Add your custom services
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<AuthService>(); // ✅ NEW: Authentication service
builder.Services.AddScoped<DashboardService>(); // ✅ NEW: Dashboard service
builder.Services.AddScoped<RegistrationService>(); // ✅ NEW: Registration service
builder.Services.AddScoped<TicketService>(); // ✅ NEW: Ticket service
builder.Services.AddScoped<ProfileService>(); // ✅ NEW: Profile service
builder.Services.AddScoped<EventAssistantService>(); // ✅ NEW: Event Assistant service

// ✅ ADD AUTHORIZATION
builder.Services.AddAuthorizationCore();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();