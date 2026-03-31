using Dapper;
using Microsoft.Data.SqlClient;
using StaytrustAdmin.Models;

namespace StaytrustAdmin.Services;

public class NotificationService : INotificationService
{
    private readonly string _connectionString;

    public NotificationService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

    // ─── STATS ────────────────────────────────────────────────────────────────

    public async Task<NotificationStats> GetStatsAsync()
    {
        const string sql = @"
            SELECT
                COUNT(*)                                                    AS TotalSent,
                SUM(CASE WHEN IsRead = 0 THEN 1 ELSE 0 END)                AS TotalUnread,
                SUM(CASE WHEN IsRead = 1 THEN 1 ELSE 0 END)                AS TotalRead,
                SUM(CASE WHEN CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END) AS SentToday
            FROM Notifications;";

        using var conn = CreateConnection();
        return await conn.QueryFirstAsync<NotificationStats>(sql);
    }

    // ─── SEND ─────────────────────────────────────────────────────────────────

    public async Task<int> SendBroadcastAsync(string title, string message, string roleFilter)
    {
        var roleWhere = roleFilter.ToLower() switch
        {
            "tenant"   => "WHERE [Role] = 'Tenant'",
            "landlord" => "WHERE [Role] = 'Landlord'",
            "admin"    => "WHERE [Role] = 'Admin'",
            _          => ""
        };

        var sql = $@"
            INSERT INTO Notifications (UserId, Title, Message, IsRead, CreatedAt)
            SELECT UserId, @Title, @Message, 0, GETDATE()
            FROM Users {roleWhere};
            SELECT @@ROWCOUNT;";

        using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(sql, new { Title = title, Message = message });
        return count;
    }

    public async Task SendToUserAsync(string title, string message, int userId)
    {
        const string sql = @"
            INSERT INTO Notifications (UserId, Title, Message, IsRead, CreatedAt)
            VALUES (@UserId, @Title, @Message, 0, GETDATE());";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { UserId = userId, Title = title, Message = message });
    }

    // ─── LIST ─────────────────────────────────────────────────────────────────

    public async Task<(List<NotificationItem> Items, int TotalCount)> GetNotificationsAsync(
        string? search, bool? isRead, int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
            conditions.Add("(n.Title LIKE @Search OR n.Message LIKE @Search OR u.FullName LIKE @Search)");
        if (isRead.HasValue)
            conditions.Add($"n.IsRead = {(isRead.Value ? 1 : 0)}");

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var sql = $@"
            SELECT n.*,
                   u.FullName  AS RecipientName,
                   u.[Role]    AS RecipientRole,
                   u.Email     AS RecipientEmail
            FROM Notifications n
            LEFT JOIN Users u ON n.UserId = u.UserId
            {where}
            ORDER BY n.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) FROM Notifications n
            LEFT JOIN Users u ON n.UserId = u.UserId
            {where};";

        using var conn = CreateConnection();
        using var multi = await conn.QueryMultipleAsync(sql, new
        {
            Search = $"%{search}%",
            Offset = offset,
            PageSize = pageSize
        });

        var items = (await multi.ReadAsync<NotificationItem>()).ToList();
        var total = await multi.ReadFirstAsync<int>();
        return (items, total);
    }

    // ─── ACTIONS ──────────────────────────────────────────────────────────────

    public async Task MarkAsReadAsync(int notificationId)
    {
        const string sql = "UPDATE Notifications SET IsRead = 1 WHERE NotificationId = @Id;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = notificationId });
    }

    public async Task MarkAllAsReadAsync()
    {
        const string sql = "UPDATE Notifications SET IsRead = 1 WHERE IsRead = 0;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql);
    }

    public async Task DeleteAsync(int notificationId)
    {
        const string sql = "DELETE FROM Notifications WHERE NotificationId = @Id;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = notificationId });
    }

    public async Task DeleteAllReadAsync()
    {
        const string sql = "DELETE FROM Notifications WHERE IsRead = 1;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql);
    }

    // ─── USER SEARCH ──────────────────────────────────────────────────────────

    public async Task<List<(int UserId, string FullName, string Email, string Role)>> SearchUsersAsync(string query)
    {
        const string sql = @"
            SELECT TOP 10 UserId, FullName, Email, [Role]
            FROM Users
            WHERE FullName LIKE @Query OR Email LIKE @Query OR UserName LIKE @Query
            ORDER BY FullName;";
        using var conn = CreateConnection();
        var rows = await conn.QueryAsync(sql, new { Query = $"%{query}%" });
        return rows.Select(r => ((int)r.UserId, (string)r.FullName, (string)r.Email, (string)r.Role)).ToList();
    }
}
