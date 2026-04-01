using System.Text;
using System.Text.Json;

namespace STAYTRUST.Services;

public class GeminiAIService : IGeminiAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiAIService> _logger;

    public GeminiAIService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiAIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> SendMessageAsync(string userMessage, List<ChatMessage> conversationHistory)
    {
        try
        {
            var apiKey = _configuration["GeminiAI:ApiKey"];
            var endpoint = _configuration["GeminiAI:Endpoint"]
                ?? "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
            var systemPrompt = _configuration["GeminiAI:SystemPrompt"]
                ?? "Bạn là trợ lý AI thông minh của StayTrust. Hãy trả lời bằng tiếng Việt.";

            if (string.IsNullOrEmpty(apiKey))
                return "Lỗi: Chưa cấu hình API key cho Gemini AI.";

            // Build conversation contents
            var contents = new List<object>();

            // Add history
            foreach (var msg in conversationHistory)
            {
                contents.Add(new
                {
                    role = msg.Role,
                    parts = new[] { new { text = msg.Content } }
                });
            }

            // Add current user message
            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMessage } }
            });

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = contents,
                generationConfig = new
                {
                    temperature = 0.7,
                    topK = 40,
                    topP = 0.95,
                    maxOutputTokens = 1024
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{endpoint}?key={apiKey}";
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {StatusCode} - {Body}", response.StatusCode, errorBody);
                return $"Lỗi kết nối với AI: {response.StatusCode}. Vui lòng thử lại!";
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);

            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "Không có phản hồi từ AI.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini AI API");
            return "Đã xảy ra lỗi khi kết nối với trợ lý AI. Vui lòng thử lại sau!";
        }
    }
}
