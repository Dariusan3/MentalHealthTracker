using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MentalHealthTracker.Models;
using MentalHealthTracker.Services;
using Stripe;
using System.IO;
using System.Threading.Tasks;
using System.Security.Claims;

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
            Console.WriteLine("=== Am ajuns în CreateCheckoutSession ===");
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return BadRequest("Utilizatorul nu a fost găsit.");
            }

            try
            {
                var sessionUrl = await _stripeService.CreateCheckoutSessionAsync(userId, user.Email);
                return Ok(new { url = sessionUrl });
            }
            catch (Exception ex)
            {
                // Loghează excepția în consolă
                Console.WriteLine("Controller error: " + ex.ToString());
                return BadRequest("Eroare la crearea sesiunii de plată.");
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _configuration["Stripe:WebhookSecret"]
                );

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    await _stripeService.HandleCheckoutSessionCompletedAsync(stripeEvent);
                }

                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("subscription-status")]
        [Authorize]
        public async Task<IActionResult> GetSubscriptionStatus()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return BadRequest("Utilizatorul nu a fost găsit.");
            }

            return Ok(new 
            { 
                isSubscribed = user.IsSubscribed,
                messagesLeftToday = user.MessagesLeftToday 
            });
        }
    }
} 