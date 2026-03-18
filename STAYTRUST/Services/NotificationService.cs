using Microsoft.EntityFrameworkCore;
using STAYTRUST.Data;
using STAYTRUST.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace STAYTRUST.Services;

public class NotificationService : INotificationService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public NotificationService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(int userId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Notifications
            .CountAsync(n => n.UserId == userId && (n.IsRead == false || n.IsRead == null));
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        using var context = await _factory.CreateDbContextAsync();
        var notification = await context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task SendNotificationAsync(int userId, string title, string message)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.Now
        };

        using var context = await _factory.CreateDbContextAsync();
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
    }
}
