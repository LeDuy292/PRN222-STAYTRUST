using Microsoft.EntityFrameworkCore;
using STAYTRUST.Data;
using STAYTRUST.Models;

namespace STAYTRUST.Services;

public class MessageService : IMessageService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public MessageService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<Message> SendMessageAsync(int senderId, int receiverId, int? roomId, string content)
    {
        using var context = await _factory.CreateDbContextAsync();
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            RoomId = roomId,
            Content = content,
            IsRead = false,
            CreatedAt = DateTime.Now
        };

        context.Messages.Add(message);
        await context.SaveChangesAsync();
        return message;
    }

    public async Task<List<Message>> GetConversationAsync(int userId1, int userId2, int? roomId = null)
    {
        using var context = await _factory.CreateDbContextAsync();
        var query = context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2)
                     || (m.SenderId == userId2 && m.ReceiverId == userId1));

        if (roomId.HasValue)
        {
            query = query.Where(m => m.RoomId == roomId);
        }

        return await query.OrderBy(m => m.CreatedAt).ToListAsync();
    }

    public async Task<List<ConversationSummary>> GetUserConversationsAsync(int userId)
    {
        using var context = await _factory.CreateDbContextAsync();

        var messages = await context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Include(m => m.Room)
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        var conversations = messages
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g =>
            {
                var lastMsg = g.First();
                var otherUserId = g.Key;
                var otherUser = lastMsg.SenderId == otherUserId ? lastMsg.Sender : lastMsg.Receiver;

                return new ConversationSummary
                {
                    OtherUserId = otherUserId,
                    OtherUserName = otherUser?.FullName ?? otherUser?.UserName ?? "User",
                    LastMessage = lastMsg.Content,
                    LastMessageTime = lastMsg.CreatedAt,
                    UnreadCount = g.Count(m => m.ReceiverId == userId && m.IsRead != true),
                    RoomId = lastMsg.RoomId,
                    RoomTitle = lastMsg.Room?.Title
                };
            })
            .ToList();

        return conversations;
    }
}
