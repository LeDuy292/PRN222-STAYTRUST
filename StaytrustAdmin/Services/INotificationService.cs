using StaytrustAdmin.Models;

namespace StaytrustAdmin.Services;

public interface INotificationService
{
    // ─── Stats ────────────────────────────────────────────────────────────────
    Task<NotificationStats> GetStatsAsync();

    // ─── Send ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// Send notification to all users of a given role, or all roles if roleFilter = "all"
    /// </summary>
    Task<int> SendBroadcastAsync(string title, string message, string roleFilter);

    /// <summary>
    /// Send notification to a single user by UserId
    /// </summary>
    Task SendToUserAsync(string title, string message, int userId);

    // ─── List / Filter ────────────────────────────────────────────────────────
    Task<(List<NotificationItem> Items, int TotalCount)> GetNotificationsAsync(
        string? search, bool? isRead, int page, int pageSize);

    // ─── Actions ──────────────────────────────────────────────────────────────
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync();
    Task DeleteAsync(int notificationId);
    Task DeleteAllReadAsync();

    // ─── User search (for Send-to-User) ──────────────────────────────────────
    Task<List<(int UserId, string FullName, string Email, string Role)>> SearchUsersAsync(string query);
}
