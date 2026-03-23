using System.Text.Json;
using System.Text.Json.Serialization;

namespace STAYTRUST.Services
{
    public interface IGeminiAIService
    {
        Task<string> GenerateResponseAsync(string userMessage);
        Task<string> GetSmartRecommendationAsync(string context);
    }

    public class GeminiAIService : IGeminiAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiAIService> _logger;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _model;

        public GeminiAIService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiAIService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = configuration["GeminiAI:ApiKey"] ?? "";
            _endpoint = configuration["GeminiAI:Endpoint"] ?? "";
            _model = configuration["GeminiAI:Model"] ?? "gemini-1.5-flash";
        }

        public async Task<string> GenerateResponseAsync(string userMessage)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(userMessage))
                {
                    return "Xin l?i, tôi không hi?u câu h?i c?a b?n. Vui lòng th? l?i.";
                }

                // Prepare system prompt for better context
                var systemPrompt = @"B?n là m?t tr? lý h? tr? khách hàng thông minh cho ?ng d?ng qu?n lý phòng cho thuê STAYTRUST. 
B?n nên:
1. Tr? l?i b?ng ti?ng Vi?t m?t cách l?ch s? và chuyên nghi?p
2. Giúp ng??i dùng v?i các câu h?i v? ?ng d?ng, quy trình thuê phòng, thanh toán
3. Cung c?p l?i khuyên h?u ích v? b?t ??ng s?n
4. Luôn gi? tính tích c?c và h? tr? t?i ?a
5. N?u không bi?t câu tr? l?i, hãy ?? xu?t liên h? v?i b? ph?n h? tr?

L?u ý: B?n ?ang h? tr? cho ?ng d?ng STAYTRUST - m?t n?n t?ng qu?n lý và cho thuê phòng.";

                var request = new GeminiRequest
                {
                    Contents = new List<GeminiContent>
                    {
                        new GeminiContent
                        {
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart { Text = systemPrompt },
                                new GeminiPart { Text = userMessage }
                            }
                        }
                    },
                    GenerationConfig = new GenerationConfig
                    {
                        Temperature = 0.7f,
                        MaxOutputTokens = 500,
                        TopP = 0.9f,
                        TopK = 40
                    }
                };

                var url = $"{_endpoint}?key={_apiKey}";
                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (geminiResponse?.Candidates?.Count > 0 && 
                        geminiResponse.Candidates[0].Content?.Parts?.Count > 0)
                    {
                        var generatedText = geminiResponse.Candidates[0].Content.Parts[0].Text;
                        return string.IsNullOrWhiteSpace(generatedText) 
                            ? "Xin l?i, tôi không th? t?o câu tr? l?i. Vui lòng th? l?i."
                            : generatedText;
                    }
                }

                _logger.LogWarning($"Gemini API returned status code: {response.StatusCode}");
                return "Xin l?i, ?ã x?y ra l?i khi x? lý câu h?i c?a b?n. Vui lòng th? l?i sau.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GenerateResponseAsync: {ex.Message}");
                return "Xin l?i, ?ã x?y ra l?i. Vui lòng liên h? v?i b? ph?n h? tr?.";
            }
        }

        public async Task<string> GetSmartRecommendationAsync(string context)
        {
            try
            {
                var prompt = $@"D?a trên b?i c?nh sau, hãy ??a ra l?i khuyên thông minh:
B?i c?nh: {context}

Vui lòng cung c?p l?i khuyên ng?n g?n, h?u ích và d? hi?u.";

                return await GenerateResponseAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetSmartRecommendationAsync: {ex.Message}");
                return "Không th? l?y khuy?n ngh? vào lúc này.";
            }
        }
    }

    #region Gemini API Models

    public class GeminiRequest
    {
        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = new();

        [JsonPropertyName("generationConfig")]
        public GenerationConfig GenerationConfig { get; set; } = new();
    }

    public class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new();
    }

    public class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class GenerationConfig
    {
        [JsonPropertyName("temperature")]
        public float Temperature { get; set; } = 0.7f;

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; } = 500;

        [JsonPropertyName("topP")]
        public float TopP { get; set; } = 0.9f;

        [JsonPropertyName("topK")]
        public int TopK { get; set; } = 40;
    }

    public class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate> Candidates { get; set; } = new();
    }

    public class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent Content { get; set; } = new();

        [JsonPropertyName("finishReason")]
        public string FinishReason { get; set; } = string.Empty;
    }

    #endregion
}
