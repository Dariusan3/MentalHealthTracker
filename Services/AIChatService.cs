using Microsoft.Extensions.Options;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using MentalHealthTracker.Services;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MentalHealthTracker.Services
{
    public class AIChatService
    {
        public AIChatService() { }

        public async Task<string> GetChatResponseOllamaAsync(string prompt, IServiceProvider serviceProvider = null)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(120);

                var context = "You are a virtual assistant specialized in mental health. Respond empathetically, with useful advice, and validate the user's emotions. Please always respond in English. ";
                var fullPrompt = context + prompt;

                var json = JsonSerializer.Serialize(new
                {
                    model = "llama3:latest",
                    prompt = fullPrompt,
                    stream = false
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("http://localhost:11434/api/generate", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    return "Error: The AI service reported a problem (Status: " + response.StatusCode + ").";
                }

                var result = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(result);
                string aiResponse = "";
                if (doc.RootElement.TryGetProperty("response", out var responseProp))
                {
                    aiResponse = responseProp.GetString() ?? string.Empty;
                }
                else
                {
                    return "Error: The response received from AI is not in the expected format.";
                }

                // Trimite notificare SignalR dacă serviceProvider este furnizat
                if (serviceProvider != null)
                {
                    var hubContext = serviceProvider.GetService(typeof(IHubContext<NotificationHub>)) as IHubContext<NotificationHub>;
                    var httpContextAccessor = serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
                    var userId = httpContextAccessor?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (hubContext != null && userId != null)
                    {
                        await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", "✅ AI has generated a response for your journal entry.");
                    }
                }

                return aiResponse;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Connection error to Ollama: {ex.Message}");
                return "Error: Could not connect to local AI service (Ollama). Please ensure the Ollama application is running.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in AIChatService: {ex.Message}");
                return "Unexpected error communicating with AI: " + ex.Message;
            }
        }
    }
}