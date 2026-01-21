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
                                UnitAmount = 999,
                                Currency = "ron",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "MentalHealthTracker Premium Subscription",
                                    Description = "Unlimited access to our AI-powered chatbot"
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

                // Store session ID for later verification
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.StripeCustomerId = session.CustomerId;
                    await _userManager.UpdateAsync(user);
                    Console.WriteLine($"Stripe session created for user {user.Email} with ID: {session.Id}");
                }

                return session.Url;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Stripe error: " + ex.ToString());
                throw;
            }
        }

        public async Task<bool> VerifyAndActivateSubscription(string sessionId)
        {
            try
            {
                var service = new SessionService();
                var session = await service.GetAsync(sessionId);
                
                if (session == null)
                {
                    Console.WriteLine($"Session {sessionId} not found");
                    return false;
                }

                if (session.PaymentStatus != "paid")
                {
                    Console.WriteLine($"Session {sessionId} is not paid. Status: {session.PaymentStatus}");
                    return false;
                }

                var userId = session.Metadata["UserId"];
                var user = await _userManager.FindByIdAsync(userId);
                
                if (user == null)
                {
                    Console.WriteLine($"User with ID {userId} not found");
                    return false;
                }

                user.IsSubscribed = true;
                user.StripeCustomerId = session.CustomerId;
                user.MessagesLeftToday = int.MaxValue;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    Console.WriteLine($"Subscription successfully activated for user {user.Email}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Error updating user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying session: {ex.Message}");
                return false;
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

            // Check if user is subscribed, in which case we don't decrement
            if (user.IsSubscribed) return true;

            // Check if we need to reset message counter (at least one day has passed)
            if (user.LastMessageResetDate == null || user.LastMessageResetDate.Value.Date < DateTime.Today)
            {
                user.MessagesLeftToday = 5;
                user.LastMessageResetDate = DateTime.Today;
            }

            // Check if user has messages available
            if (user.MessagesLeftToday <= 0) return false;

            // Decrement message count and save
            user.MessagesLeftToday--;
            await _userManager.UpdateAsync(user);
            return true;
        }
    }
} 