using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MentalHealthTracker.Data;
using MentalHealthTracker.Models;

public class ChatHistoryService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChatHistoryService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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