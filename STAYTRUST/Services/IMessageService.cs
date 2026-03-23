using STAYTRUST.Models;

namespace STAYTRUST.Services;

public interface IMessageService
{
    Task<Message> SendMessageAsync(int senderId, int receiverId, int? roomId, string content);
    Task<List<Message>> GetConversationAsync(int userId1, int userId2, int? roomId = null);
    Task<List<ConversationSummary>> GetUserConversationsAsync(int userId);
}

public class ConversationSummary
{
    public int OtherUserId { get; set; }
    public string OtherUserName { get; set; } = "";
    public string LastMessage { get; set; } = "";
    public DateTime? LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
    public int? RoomId { get; set; }
    public string? RoomTitle { get; set; }
}
