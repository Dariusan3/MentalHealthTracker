using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using MentalHealthTracker.Data;
using MentalHealthTracker.Services;
using MentalHealthTracker.Models;
using MentalHealthTracker.Areas.Identity;
using System.Security.Claims;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.SemanticKernel;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// // AI setup
// builder.Services.AddKernel();
// var aiConfig = builder.Configuration.GetSection("AIChat");
// builder.Services.AddAzureOpenAIChatCompletion(
//     deploymentName: aiConfig["DeploymentName"],
//     endpoint: aiConfig["Endpoint"],
//     new DefaultAzureCredential()
// );

// Adăugăm un CookieContainer partajat pentru toate instanțele HttpClient
var cookieContainer = new System.Net.CookieContainer();

// Adăugăm suport pentru CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.WithOrigins("https://localhost:5001", "https://localhost:7214")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp => 
{
    var httpClient = new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
        UseCookies = true,
        CookieContainer = cookieContainer,
        UseDefaultCredentials = true,
        AllowAutoRedirect = true
    });
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    httpClient.BaseAddress = new Uri(navigationManager.BaseUri);
    
    // Adăugăm un handler pentru a afișa cookie-urile în fiecare cerere
    httpClient.DefaultRequestHeaders.Add("X-Debug", "true");
    
    return httpClient;
});

// Configurare Entity Framework și Identity
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Configurări pentru parole
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    
    // Configurări pentru utilizator
    options.User.RequireUniqueEmail = true;
    
    // Configurări pentru blocare cont
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configurare autentificare și autorizare
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();

// Configurare cookie-uri pentru autentificare
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/access-denied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Cookie.Name = "MentalHealthTracker.Auth";
    
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// Adăugare servicii aplicație
builder.Services.AddScoped<MoodService>();
builder.Services.AddSingleton<AIChatService>();
builder.Services.AddScoped<ChatHistoryService>();
builder.Services.AddScoped<UserProfileService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Adăugăm CORS înainte de UseRouting
app.UseCors("AllowAll");

app.UseRouting();

// Adăugare middleware pentru autentificare și autorizare
app.UseAuthentication();
app.UseAuthorization();

// Adăugăm middleware pentru a verifica cookie-urile
app.Use(async (context, next) =>
{
    // Verificăm cookie-urile pentru cererile relevante
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var cookies = context.Request.Cookies;
        var cookieNames = string.Join(", ", cookies.Keys);
        var authCookie = cookies.FirstOrDefault(c => c.Key.Contains(".Auth") || c.Key.Contains("Identity"));
        
        Console.WriteLine($"[{DateTime.Now}] Cerere: {context.Request.Path}, Cookies: {cookieNames}");
        
        if (!string.IsNullOrEmpty(authCookie.Key))
        {
            Console.WriteLine($"[{DateTime.Now}] Cookie autentificare găsit: {authCookie.Key}");
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now}] ATENȚIE: Nu s-a găsit cookie de autentificare pentru cererea API!");
        }
    }
    
    await next();
});

// Adăugăm middleware pentru diagnosticarea autentificării
app.Use(async (context, next) =>
{
    // Logăm informații despre autentificare pentru fiecare cerere API
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
        var userName = context.User.Identity?.Name ?? "necunoscut";
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "necunoscut";
        Console.WriteLine($"[{DateTime.Now}] Cerere: {context.Request.Path}, Autentificat: {isAuthenticated}, Utilizator: {userName}, ID: {userId}");
    }
    
    await next();
});

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Inițializare bază de date
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Asigurăm-ne că baza de date este creată
        context.Database.EnsureCreated();
        
        // Aplicăm migrările dacă există
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "A apărut o eroare la inițializarea bazei de date.");
    }
}

app.Run();
