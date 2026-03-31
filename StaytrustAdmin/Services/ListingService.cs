using Dapper;
using Microsoft.Data.SqlClient;
using StaytrustAdmin.Models;

namespace StaytrustAdmin.Services;

public class ListingService : IListingService
{
    private readonly string _connectionString;

    public ListingService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

    // ─── LANDLORD VIEW ────────────────────────────────────────────────────────

    public async Task<List<LandlordSummary>> GetLandlordsAsync(string? search)
    {
        var whereSearch = string.IsNullOrWhiteSpace(search)
            ? ""
            : "AND (u.FullName LIKE @Search OR u.Email LIKE @Search OR u.Phone LIKE @Search)";

        var sql = $@"
            SELECT
                u.UserId,
                u.FullName,
                u.Email,
                u.Phone,
                u.Status      AS IsActive,
                u.CreatedAt,
                up.AvatarUrl,
                COUNT(r.RoomId)                                                AS TotalRooms,
                SUM(CASE WHEN r.Status IN ('Active','Available','Rented') THEN 1 ELSE 0 END) AS ActiveRooms,
                SUM(CASE WHEN r.Status IN ('Hidden','Draft','Expired')    THEN 1 ELSE 0 END) AS HiddenRooms,
                SUM(CASE WHEN r.CreatedAt >= DATEADD(HOUR,-24,GETDATE())  THEN 1 ELSE 0 END) AS RoomsPostedLast24h,
                ISNULL(AVG(r.Rating), 0)                                       AS AvgRating
            FROM Users u
            LEFT JOIN UserProfiles up ON up.UserId = u.UserId
            LEFT JOIN Rooms r ON r.LandlordId = u.UserId
            WHERE u.Role = 'Landlord'
            {whereSearch}
            GROUP BY u.UserId, u.FullName, u.Email, u.Phone, u.Status, u.CreatedAt, up.AvatarUrl
            ORDER BY RoomsPostedLast24h DESC, u.FullName ASC;";

        using var conn = CreateConnection();
        var result = await conn.QueryAsync<LandlordSummary>(sql, new { Search = $"%{search}%" });
        return result.ToList();
    }

    public async Task BlockLandlordAsync(int userId, bool block)
    {
        const string sql = "UPDATE Users SET [Status] = @Status WHERE UserId = @UserId;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Status = !block, UserId = userId });
    }

    // ─── ROOM VIEW BY LANDLORD ────────────────────────────────────────────────

    public async Task<(List<Room> Rooms, int TotalCount)> GetRoomsByLandlordAsync(
        int landlordId, string? status, int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;
        var statusFilter = "";
        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            if (status == "active")   statusFilter = "AND r.Status IN ('Active','Available','Rented')";
            if (status == "hidden")   statusFilter = "AND r.Status IN ('Hidden','Draft','Expired')";
        }

        var sql = $@"
            SELECT r.*,
                   u.FullName AS LandlordName,
                   (SELECT TOP 1 ImageUrl FROM RoomImages i WHERE i.RoomId = r.RoomId ORDER BY i.CreatedAt DESC) AS DefaultImageUrl
            FROM Rooms r
            LEFT JOIN Users u ON r.LandlordId = u.UserId
            WHERE r.LandlordId = @LandlordId {statusFilter}
            ORDER BY r.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) FROM Rooms r WHERE r.LandlordId = @LandlordId {statusFilter};";

        using var conn = CreateConnection();
        using var multi = await conn.QueryMultipleAsync(sql, new { LandlordId = landlordId, Offset = offset, PageSize = pageSize });
        var rooms  = (await multi.ReadAsync<Room>()).ToList();
        var total  = await multi.ReadFirstAsync<int>();
        return (rooms, total);
    }

    // ─── GLOBAL LISTING VIEW (all landlords) ─────────────────────────────────

    public async Task<(List<Room> Rooms, int TotalCount)> GetListingsAsync(
        string? search, string? status, int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
            conditions.Add("(r.Title LIKE @Search OR u.FullName LIKE @Search)");

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            if (status == "active")  conditions.Add("r.Status IN ('Active','Available','Rented')");
            if (status == "hidden")  conditions.Add("r.Status IN ('Hidden','Draft','Expired')");
            if (status == "spam")    conditions.Add("r.LandlordId IN (SELECT LandlordId FROM Rooms GROUP BY LandlordId HAVING SUM(CASE WHEN CreatedAt >= DATEADD(HOUR,-24,GETDATE()) THEN 1 ELSE 0 END) >= 3)");
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var sql = $@"
            SELECT r.*,
                   u.FullName AS LandlordName,
                   (SELECT TOP 1 ImageUrl FROM RoomImages i WHERE i.RoomId = r.RoomId ORDER BY i.CreatedAt DESC) AS DefaultImageUrl
            FROM Rooms r
            LEFT JOIN Users u ON r.LandlordId = u.UserId
            {where}
            ORDER BY r.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) FROM Rooms r LEFT JOIN Users u ON r.LandlordId = u.UserId {where};";

        using var conn = CreateConnection();
        using var multi = await conn.QueryMultipleAsync(sql, new { Search = $"%{search}%", Offset = offset, PageSize = pageSize });
        var rooms = (await multi.ReadAsync<Room>()).ToList();
        var total = await multi.ReadFirstAsync<int>();
        return (rooms, total);
    }

    public async Task<(int Total, int Active, int Hidden, int SpamFlagged)> GetListingStatsAsync()
    {
        const string sql = @"
            SELECT
                COUNT(*)                                                                         AS Total,
                SUM(CASE WHEN Status IN ('Active','Available','Rented') THEN 1 ELSE 0 END)      AS Active,
                SUM(CASE WHEN Status IN ('Hidden','Draft','Expired')    THEN 1 ELSE 0 END)      AS Hidden,
                (SELECT COUNT(DISTINCT LandlordId) FROM Rooms
                 GROUP BY LandlordId
                 HAVING SUM(CASE WHEN CreatedAt >= DATEADD(HOUR,-24,GETDATE()) THEN 1 ELSE 0 END) >= 3) AS SpamFlagged
            FROM Rooms;";

        using var conn = CreateConnection();
        var stats = await conn.QueryFirstAsync(sql);
        return ((int)stats.Total, (int)stats.Active, (int)stats.Hidden, (int)(stats.SpamFlagged ?? 0));
    }

    public async Task UpdateListingStatusAsync(int roomId, string newStatus)
    {
        var finalStatus = newStatus switch {
            "active"  => "Active",
            "hide"    => "Hidden",
            "approve" => "Active",
            "reject"  => "Hidden",
            _         => newStatus
        };
        const string sql = "UPDATE Rooms SET Status = @Status, UpdatedAt = GETDATE() WHERE RoomId = @RoomId;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Status = finalStatus, RoomId = roomId });
    }

    public async Task DeleteListingAsync(int roomId)
    {
        const string sql = @"
            BEGIN TRANSACTION;
            BEGIN TRY
                DELETE p FROM Payments p INNER JOIN Invoices i ON p.InvoiceId = i.InvoiceId INNER JOIN RentalContracts c ON i.ContractId = c.ContractId WHERE c.RoomId = @RoomId;
                DELETE i FROM Invoices i INNER JOIN RentalContracts c ON i.ContractId = c.ContractId WHERE c.RoomId = @RoomId;
                DELETE FROM RentalContracts WHERE RoomId = @RoomId;
                DELETE FROM RoomImages    WHERE RoomId = @RoomId;
                DELETE FROM Feedbacks     WHERE RoomId = @RoomId;
                DELETE FROM FavoriteRooms WHERE RoomId = @RoomId;
                DELETE FROM UtilityRates  WHERE RoomId = @RoomId;
                DELETE FROM MeterReadings WHERE RoomId = @RoomId;
                DELETE FROM Messages      WHERE RoomId = @RoomId;
                DELETE FROM Rooms         WHERE RoomId = @RoomId;
                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                ROLLBACK TRANSACTION;
                THROW;
            END CATCH";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { RoomId = roomId });
    }
}
