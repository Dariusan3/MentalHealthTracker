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

        public async Task<ChatConversation> CreateNewConversationAsync(string title = "Conversație nouă")
        {
            var userId = GetCurrentUserId();
            if (userId == null) throw new UnauthorizedAccessException("Utilizatorul nu este autentificat.");

            var conversation = new ChatConversation
            {
                UserId = userId,
                Title = title,
                CreatedAt = DateTime.UtcNow
            };

            _db.ChatConversations.Add(conversation);
            await _db.SaveChangesAsync();
            return conversation;
        }

        public async Task DeleteConversationAsync(int conversationId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) throw new UnauthorizedAccessException("Utilizatorul nu este autentificat.");

            var conversation = await _db.ChatConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

            if (conversation != null)
            {
                // Șterge toate mesajele asociate conversației
                _db.ChatMessages.RemoveRange(conversation.Messages);
                // Șterge conversația
                _db.ChatConversations.Remove(conversation);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<ChatConversation>> GetUserConversationsAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return new List<ChatConversation>();

            return await _db.ChatConversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.LastModifiedAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task<ChatConversation?> GetConversationWithMessagesAsync(int conversationId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return null;

            return await _db.ChatConversations
                .Include(c => c.Messages.OrderBy(m => m.Timestamp))
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);
        }

        public async Task AddMessageToConversationAsync(int conversationId, string role, string content)
        {
            var userId = GetCurrentUserId();
            if (userId == null) throw new UnauthorizedAccessException("Utilizatorul nu este autentificat.");

            var conversation = await _db.ChatConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

            if (conversation == null) throw new InvalidOperationException("Conversația nu a fost găsită.");

            var message = new ChatMessage
            {
                UserId = userId,
                Role = role,
                Content = content,
                ConversationId = conversationId,
                Timestamp = DateTime.UtcNow
            };

            conversation.LastModifiedAt = DateTime.UtcNow;
            _db.ChatMessages.Add(message);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateConversationTitleAsync(int conversationId, string newTitle)
        {
            var userId = GetCurrentUserId();
            if (userId == null) throw new UnauthorizedAccessException("Utilizatorul nu este autentificat.");

            var conversation = await _db.ChatConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

            if (conversation != null)
            {
                conversation.Title = newTitle;
                conversation.LastModifiedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
    }
} 