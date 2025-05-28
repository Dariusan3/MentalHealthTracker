using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MentalHealthTracker.Data;
using MentalHealthTracker.Models;
using Microsoft.AspNetCore.Identity;
using MentalHealthTracker.Services;

namespace MentalHealthTracker.Services
{
    public class ChatHistoryService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StripeService _stripeService;

        public ChatHistoryService(
            ApplicationDbContext db, 
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
            StripeService stripeService)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _stripeService = stripeService;
        }

        public string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public async Task<(bool IsAllowed, string ErrorMessage)> CanUserSendMessageAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null) 
                return (false, "Utilizatorul nu este autentificat.");

            // Verifică dacă utilizatorul poate trimite un mesaj (fie este abonat, fie mai are mesaje disponibile)
            var canSendMessage = await _stripeService.DecrementMessageCountAsync(userId);
            
            if (!canSendMessage)
            {
                return (false, "Ai atins limita zilnică. Upgradează pentru acces nelimitat.");
            }

            return (true, string.Empty);
        }

        public async Task AddMessageAsync(string role, string content)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            var message = new ChatMessage
            {
                UserId = userId,
                Role = role,
                Content = content
            };
            _db.ChatMessages.Add(message);
            await _db.SaveChangesAsync();
        }

        public async Task<List<ChatMessage>> GetHistoryAsync(int limit = 20)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return new List<ChatMessage>();

            return await _db.ChatMessages
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<List<ChatMessage>?> GetChatHistoryForUserAsync(string userId)
        {
            return await _db.ChatMessages
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.Timestamp)
                .Take(20)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }
    }
} 