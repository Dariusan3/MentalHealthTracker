using Microsoft.Extensions.Options;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using MentalHealthTracker.Services;

public class AIChatService
{
    public AIChatService() { }

    public async Task<string> GetChatResponseOllamaAsync(string prompt, IServiceProvider serviceProvider = null)
    {
        var client = new HttpClient();
        var context = "Ești un asistent virtual specializat în sănătate mintală. Răspunde empatic, cu sfaturi utile și validează emoțiile utilizatorului. ";
        var fullPrompt = context + prompt;

        var json = JsonSerializer.Serialize(new
        {
            model = "llama3.2",
            prompt = fullPrompt,
            stream = false
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("http://localhost:11434/api/generate", content);
        var result = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(result);
        string aiResponse = result;
        if (doc.RootElement.TryGetProperty("response", out var responseProp))
        {
            aiResponse = responseProp.GetString() ?? string.Empty;
        }

        // Trimite notificare SignalR dacă serviceProvider este furnizat
        if (serviceProvider != null)
        {
            var hubContext = serviceProvider.GetService(typeof(IHubContext<NotificationHub>)) as IHubContext<NotificationHub>;
            var httpContextAccessor = serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
            var userId = httpContextAccessor?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (hubContext != null && userId != null)
            {
                await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", "✅ AI a generat răspunsul pentru jurnalul tău.");
            }
        }

        return aiResponse;
    }
}