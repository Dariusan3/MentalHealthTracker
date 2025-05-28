using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MentalHealthTracker.Services
{
    public class DailyMessageResetService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyMessageResetService> _logger;

        public DailyMessageResetService(
            IServiceProvider serviceProvider,
            ILogger<DailyMessageResetService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Serviciul de resetare zilnică a mesajelor a fost pornit.");

            // Calculăm timpul până la următoarea resetare (miezul nopții)
            var now = DateTime.Now;
            var nextMidnight = now.Date.AddDays(1);
            var timeUntilMidnight = nextMidnight - now;

            // Așteptăm până la miezul nopții
            await Task.Delay(timeUntilMidnight, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Resetăm contoarele de mesaje
                    await ResetDailyMessageCounters();
                    _logger.LogInformation("Resetare zilnică a contoarelor de mesaje efectuată cu succes la {time}", DateTime.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Eroare la resetarea contoarelor de mesaje");
                }

                // Așteptăm 24 de ore până la următoarea resetare
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task ResetDailyMessageCounters()
        {
            using var scope = _serviceProvider.CreateScope();
            var stripeService = scope.ServiceProvider.GetRequiredService<StripeService>();
            await stripeService.ResetDailyMessagesAsync();
        }
    }
} 