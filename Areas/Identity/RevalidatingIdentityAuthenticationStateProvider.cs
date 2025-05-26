using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MentalHealthTracker.Areas.Identity
{
    public class RevalidatingIdentityAuthenticationStateProvider<TUser>
        : RevalidatingServerAuthenticationStateProvider where TUser : class
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IdentityOptions _options;
        private readonly ILogger _logger;

        public RevalidatingIdentityAuthenticationStateProvider(
            ILoggerFactory loggerFactory,
            IServiceScopeFactory scopeFactory,
            IOptions<IdentityOptions> optionsAccessor)
            : base(loggerFactory)
        {
            _scopeFactory = scopeFactory;
            _options = optionsAccessor.Value;
            _logger = loggerFactory.CreateLogger<RevalidatingIdentityAuthenticationStateProvider<TUser>>();
            _logger.LogInformation("RevalidatingIdentityAuthenticationStateProvider inițializat");
        }

        // Reducem intervalul de revalidare la 10 secunde pentru a detecta mai rapid schimbările
        protected override TimeSpan RevalidationInterval => TimeSpan.FromSeconds(10);

        protected override async Task<bool> ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            // Obținem identitatea principală a utilizatorului
            var identity = authenticationState.User.Identity;
            if (identity == null || !identity.IsAuthenticated)
            {
                _logger.LogInformation("Utilizatorul nu este autentificat");
                return false;
            }

            var userId = authenticationState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Nu s-a găsit ID-ul utilizatorului în claims");
                return false;
            }

            _logger.LogInformation("Validare stare autentificare pentru utilizatorul cu ID {UserId}", userId);

            try
            {
                // Verificăm dacă utilizatorul există și este valid
                using var scope = _scopeFactory.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
                var user = await userManager.FindByIdAsync(userId);
                
                // Verificăm dacă utilizatorul există și nu este blocat
                var isValid = user != null && !userManager.IsLockedOutAsync(user).Result;
                
                if (!isValid)
                {
                    _logger.LogWarning("Utilizatorul cu ID {UserId} nu mai este valid.", userId);
                }
                else
                {
                    _logger.LogInformation("Utilizatorul cu ID {UserId} este valid.", userId);
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare la validarea stării de autentificare pentru utilizatorul cu ID {UserId}.", userId);
                return false;
            }
        }
        
        // Adăugăm o metodă pentru a forța revalidarea stării de autentificare
        public void ForceRevalidate()
        {
            _logger.LogInformation("Forțare revalidare stare autentificare");
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
} 