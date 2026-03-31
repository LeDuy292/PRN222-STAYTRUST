namespace StaytrustAdmin.Models;

public class LandlordSummary
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Aggregated from Rooms
    public int TotalRooms { get; set; }
    public int ActiveRooms { get; set; }
    public int HiddenRooms { get; set; }
    
    /// <summary>
    /// Số phòng được đăng trong 24h gần nhất — dùng để phát hiện spam
    /// </summary>
    public int RoomsPostedLast24h { get; set; }
    
    /// <summary>
    /// True nếu chủ trọ đang bị cảnh báo spam (đăng > 3 phòng trong 24h)
    /// </summary>
    public bool IsSpamFlagged => RoomsPostedLast24h >= 3;

    /// <summary>
    /// True nếu tài khoản đang bị khoá (Status = 0)
    /// </summary>
    public bool IsBlocked => !IsActive;

    /// <summary>
    /// Rating trung bình các phòng
    /// </summary>
    public double AvgRating { get; set; }
}
