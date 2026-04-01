namespace STAYTRUST.Services;

public interface IGeminiAIService
{
    Task<string> SendMessageAsync(string userMessage, List<ChatMessage> conversationHistory);
}

public class ChatMessage
{
    public string Role { get; set; } = "user"; // "user" or "model"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
