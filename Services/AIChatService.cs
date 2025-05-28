using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using MentalHealthTracker.Services;

public class AIChatService
{
    private readonly ChatClient _client;
    private readonly string _model = "mistral-7b-v02-int4-cpu";
    private readonly string _baseUrl = "http://localhost:5272/v1/";
    private readonly string _apiKey = "unused";

    public AIChatService()
    {
        var options = new OpenAIClientOptions();
        options.Endpoint = new Uri(_baseUrl);
        ApiKeyCredential credential = new ApiKeyCredential(_apiKey);
        _client = new OpenAIClient(credential, options).GetChatClient(_model);
    }

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