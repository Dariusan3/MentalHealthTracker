using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text;
using System.Net.Http;
using System.Text.Json;

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

    public async Task<string> GetChatResponseOllamaAsync(string prompt)
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
        if (doc.RootElement.TryGetProperty("response", out var responseProp))
        {
            return responseProp.GetString() ?? string.Empty;
        }
        return result;
    }
}