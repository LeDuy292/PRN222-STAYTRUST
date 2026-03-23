using System.Collections.Generic;
using System.Threading.Tasks;
using STAYTRUST.Models;

namespace STAYTRUST.Services;

public interface INotificationService
{
    Task<List<Notification>> GetUserNotificationsAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId);
    Task SendNotificationAsync(int userId, string title, string message);
}
