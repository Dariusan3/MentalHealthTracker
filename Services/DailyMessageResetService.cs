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
            _logger.LogInformation("Daily message reset service started.");

            // Calculate time until next reset (midnight)
            var now = DateTime.Now;
            var nextMidnight = now.Date.AddDays(1);
            var timeUntilMidnight = nextMidnight - now;

            // Wait until midnight
            await Task.Delay(timeUntilMidnight, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Reset message counters
                    await ResetDailyMessageCounters();
                    _logger.LogInformation("Daily message counters reset successfully at {time}", DateTime.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resetting message counters");
                }

                // Wait 24 hours until next reset
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