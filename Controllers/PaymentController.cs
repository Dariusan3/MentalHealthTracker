using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MentalHealthTracker.Models;
using MentalHealthTracker.Services;
using Stripe;
using Stripe.Checkout;
using System.IO;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly StripeService _stripeService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public PaymentController(
            StripeService stripeService,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _stripeService = stripeService;
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost("create-checkout-session")]
        [Authorize]
        public async Task<IActionResult> CreateCheckoutSession()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Ok(new { success = false, message = "Sesiunea a expirat. Vă rugăm să vă autentificați din nou." });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Ok(new { success = false, message = "Sesiunea a expirat. Vă rugăm să vă autentificați din nou." });
                }

                var sessionUrl = await _stripeService.CreateCheckoutSessionAsync(userId, user.Email);
                return Ok(new { success = true, url = sessionUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Controller error: " + ex.ToString());
                return Ok(new { success = false, message = "Vă rugăm să încercați din nou în câteva momente." });
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            Console.WriteLine($"Webhook primit: {json}");
            
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _configuration["Stripe:WebhookSecret"]
                );

                Console.WriteLine($"Tip eveniment Stripe: {stripeEvent.Type}");

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    Console.WriteLine("Procesare eveniment checkout.session.completed");
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null)
                    {
                        var result = await _stripeService.VerifyAndActivateSubscription(session.Id);
                        if (result)
                        {
                            Console.WriteLine("Abonament activat cu succes prin webhook");
                        }
                        else
                        {
                            Console.WriteLine("Nu s-a putut activa abonamentul prin webhook");
                        }
                    }
                }
                else if (stripeEvent.Type == "customer.subscription.created")
                {
                    Console.WriteLine("Procesare eveniment customer.subscription.created");
                    var subscription = stripeEvent.Data.Object as Subscription;
                    if (subscription != null)
                    {
                        var customerId = subscription.CustomerId;
                        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == customerId);
                        if (user != null)
                        {
                            user.IsSubscribed = true;
                            user.MessagesLeftToday = int.MaxValue;
                            await _userManager.UpdateAsync(user);
                            Console.WriteLine($"Abonament activat pentru utilizatorul {user.Email}");
                        }
                    }
                }

                return Ok();
            }
            catch (StripeException e)
            {
                Console.WriteLine($"Eroare Stripe: {e.Message}");
                return BadRequest(e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Eroare generală: {e.Message}");
                return BadRequest(e.Message);
            }
        }

        [HttpGet("subscription-status")]
        [Authorize]
        public async Task<IActionResult> GetSubscriptionStatus()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Ok(new 
                    { 
                        isSubscribed = false,
                        messagesLeftToday = 0 
                    });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Ok(new 
                    { 
                        isSubscribed = false,
                        messagesLeftToday = 0 
                    });
                }

                return Ok(new 
                { 
                    isSubscribed = user.IsSubscribed,
                    messagesLeftToday = user.MessagesLeftToday 
                });
            }
            catch (Exception ex)
            {
                return Ok(new 
                { 
                    isSubscribed = false,
                    messagesLeftToday = 0 
                });
            }
        }

        [HttpPost("verify-session/{sessionId}")]
        [Authorize]
        public async Task<IActionResult> VerifySession(string sessionId)
        {
            try
            {
                var result = await _stripeService.VerifyAndActivateSubscription(sessionId);
                if (result)
                {
                    return Ok(new { success = true, message = "Abonament activat cu succes!" });
                }
                else
                {
                    return Ok(new { success = false, message = "Nu s-a putut activa abonamentul. Vă rugăm să contactați suportul." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la verificarea sesiunii: {ex.Message}");
                return Ok(new { success = false, message = "Eroare la verificarea plății." });
            }
        }
    }
} 