using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using MentalHealthTracker.Models;
using Microsoft.Extensions.Configuration;

namespace MentalHealthTracker.Services
{
    public class StripeService
    {
        private readonly string _secretKey;
        private readonly string _publishableKey;
        private readonly string _webhookSecret;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly string _domain;

        public StripeService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            _secretKey = configuration["Stripe:SecretKey"];
            _publishableKey = configuration["Stripe:PublishableKey"];
            _webhookSecret = configuration["Stripe:WebhookSecret"];
            _userManager = userManager;
            _domain = "http://localhost:5291";

            StripeConfiguration.ApiKey = _secretKey;
        }

        public string GetPublishableKey()
        {
            return _publishableKey;
        }

        public async Task<string> CreateCheckoutSessionAsync(string userId, string email)
        {
            try
            {
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    CustomerEmail = email,
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = 999, // 9.99 în moneda specificată (ex: RON, USD, EUR)
                                Currency = "ron",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Abonament Premium MentalHealthTracker",
                                    Description = "Acces nelimitat la chatbot-ul nostru bazat pe AI"
                                },
                                Recurring = new SessionLineItemPriceDataRecurringOptions
                                {
                                    Interval = "month"
                                }
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "subscription",
                    SuccessUrl = $"{_domain}/payment/success?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{_domain}/payment/cancel",
                    Metadata = new Dictionary<string, string>
                    {
                        { "UserId", userId }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                // Stochează ID-ul sesiunii pentru a-l verifica ulterior
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    // Aici poți stoca ID-ul de sesiune dacă este necesar
                    // user.StripeSessionId = session.Id;
                    await _userManager.UpdateAsync(user);
                }

                return session.Url;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Stripe error: " + ex.ToString());
                throw;
            }
        }

        public async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return;

            var userId = session.Metadata["UserId"];
            var user = await _userManager.FindByIdAsync(userId);
            
            if (user != null)
            {
                user.IsSubscribed = true;
                user.StripeCustomerId = session.CustomerId;
                await _userManager.UpdateAsync(user);
            }
        }

        public async Task ResetDailyMessagesAsync()
        {
            var users = _userManager.Users.Where(u => !u.IsSubscribed && u.LastMessageResetDate != DateTime.Today);
            foreach (var user in users)
            {
                user.MessagesLeftToday = 5;
                user.LastMessageResetDate = DateTime.Today;
                await _userManager.UpdateAsync(user);
            }
        }

        public async Task<bool> DecrementMessageCountAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Verifică dacă utilizatorul este abonat, în acest caz nu decrementăm
            if (user.IsSubscribed) return true;

            // Verifică dacă trebuie să resetăm contorul de mesaje (a trecut cel puțin o zi)
            if (user.LastMessageResetDate == null || user.LastMessageResetDate.Value.Date < DateTime.Today)
            {
                user.MessagesLeftToday = 5;
                user.LastMessageResetDate = DateTime.Today;
            }

            // Verifică dacă utilizatorul mai are mesaje disponibile
            if (user.MessagesLeftToday <= 0) return false;

            // Decrementează numărul de mesaje și salvează
            user.MessagesLeftToday--;
            await _userManager.UpdateAsync(user);
            return true;
        }
    }
} 