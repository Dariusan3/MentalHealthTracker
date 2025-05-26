using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace MentalHealthTracker
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Configurăm serviciile de autentificare și autorizare
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
                options.AccessDeniedPath = "/account/access-denied";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax; // Schimbat de la Strict la Lax
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api") && 
                        context.Response.StatusCode == StatusCodes.Status200OK)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            
            // Adăugăm middleware pentru diagnosticarea autentificării
            app.Use(async (context, next) =>
            {
                // Logăm informații despre autentificare pentru fiecare cerere
                if (context.Request.Path.StartsWithSegments("/api/account") || 
                    context.Request.Path.Value == "/" || 
                    context.Request.Path.Value == "/account/login")
                {
                    var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
                    var userName = context.User.Identity?.Name ?? "necunoscut";
                    Console.WriteLine($"[{DateTime.Now}] Cerere: {context.Request.Path}, Autentificat: {isAuthenticated}, Utilizator: {userName}");
                }
                
                await next();
            });

            app.UseAuthentication();
            app.UseAuthorization();
        }
    }
} 