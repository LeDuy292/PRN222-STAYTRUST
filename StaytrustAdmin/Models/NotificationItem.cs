namespace StaytrustAdmin.Models;

public class NotificationItem
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    // From JOIN Users
    public string? RecipientName { get; set; }
    public string? RecipientRole { get; set; }
    public string? RecipientEmail { get; set; }
}

public class NotificationStats
{
    public int TotalSent { get; set; }
    public int TotalUnread { get; set; }
    public int TotalRead { get; set; }
    public int SentToday { get; set; }
    public double ReadRate => TotalSent == 0 ? 0 : Math.Round((double)TotalRead / TotalSent * 100, 1);
}
