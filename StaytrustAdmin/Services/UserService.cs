using Dapper;
using Microsoft.Data.SqlClient;
using StaytrustAdmin.Models;

namespace StaytrustAdmin.Services;

public class UserService : IUserService
{
    private readonly string _connectionString;

    public UserService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

    // ─── GET USERS (paged, filtered) ─────────────────────────────────────────
    public async Task<(List<User> Users, int TotalCount)> GetUsersAsync(
        string? search, string? role, bool? status, int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
            conditions.Add("(u.FullName LIKE @Search OR u.Email LIKE @Search OR u.UserName LIKE @Search)");
        if (!string.IsNullOrWhiteSpace(role) && role != "All")
            conditions.Add("u.Role = @Role");
        if (status.HasValue)
            conditions.Add("u.Status = @Status");

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var sql = $@"
            SELECT u.UserId, u.FullName, u.UserName, u.Email, u.Phone, u.Role,
                   u.Status, u.CreatedAt,
                   p.AvatarUrl, p.Gender, p.DateOfBirth, p.IdentityNumber, p.Address
            FROM Users u
            LEFT JOIN UserProfiles p ON u.UserId = p.UserId
            {where}
            ORDER BY u.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) FROM Users u {where};";

        using var conn = CreateConnection();
        using var multi = await conn.QueryMultipleAsync(sql, new
        {
            Search = $"%{search}%",
            Role = role,
            Status = status,
            Offset = offset,
            PageSize = pageSize
        });

        var users = (await multi.ReadAsync<User>()).ToList();
        var total = await multi.ReadFirstAsync<int>();
        return (users, total);
    }

    // ─── STATS ───────────────────────────────────────────────────────────────
    public async Task<(int Total, int Admins, int Landlords, int Tenants, int Active)> GetUserStatsAsync()
    {
        const string sql = @"
            SELECT
                COUNT(*)                                      AS Total,
                SUM(CASE WHEN Role='Admin'    THEN 1 ELSE 0 END) AS Admins,
                SUM(CASE WHEN Role='Landlord' THEN 1 ELSE 0 END) AS Landlords,
                SUM(CASE WHEN Role='Tenant'   THEN 1 ELSE 0 END) AS Tenants,
                SUM(CASE WHEN Status=1        THEN 1 ELSE 0 END) AS Active
            FROM Users;";

        using var conn = CreateConnection();
        var row = await conn.QueryFirstAsync(sql);
        return ((int)row.Total, (int)row.Admins, (int)row.Landlords, (int)row.Tenants, (int)row.Active);
    }

    // ─── GET BY ID ────────────────────────────────────────────────────────────
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        const string sql = @"
            SELECT u.*, p.AvatarUrl, p.Gender, p.DateOfBirth, p.IdentityNumber, p.Address
            FROM Users u
            LEFT JOIN UserProfiles p ON u.UserId = p.UserId
            WHERE u.UserId = @UserId;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
    }

    // ─── CREATE ───────────────────────────────────────────────────────────────
    public async Task CreateUserAsync(User user)
    {
        try
        {
            const string sql = @"
                INSERT INTO Users (FullName, UserName, Email, Phone, [Password], [Role], [Status], CreatedAt)
                VALUES (@FullName, @UserName, @Email, @Phone, @Password, @Role, @Status, GETDATE());
                SELECT SCOPE_IDENTITY();";

            using var conn = CreateConnection();
            var newId = await conn.ExecuteScalarAsync<int>(sql, user);

            // Create an empty profile row
            const string profileSql = @"
                INSERT INTO UserProfiles (UserId, Gender, Address, AvatarUrl)
                VALUES (@UserId, @Gender, @Address, @AvatarUrl);";
            await conn.ExecuteAsync(profileSql, new
            {
                UserId = newId,
                user.Gender,
                user.Address,
                user.AvatarUrl
            });
        }
        catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
        {
            if (ex.Message.Contains("UserName") || ex.Message.Contains("UQ__Users__C9F28456"))
                throw new Exception("This Username is already taken.");
            if (ex.Message.Contains("Email") || ex.Message.Contains("UQ__Users__A9D10534"))
                throw new Exception("This Email is already registered.");
            if (ex.Message.Contains("Phone") || ex.Message.Contains("UQ__Users__5C7E359F"))
                throw new Exception("This Phone number is already registered.");
                
            throw new Exception("A user with this unique information already exists.");
        }
    }

    // ─── UPDATE ───────────────────────────────────────────────────────────────
    public async Task UpdateUserAsync(User user)
    {
        try
        {
            const string sql = @"
                UPDATE Users
                SET FullName = @FullName,
                    UserName = @UserName,
                    Email    = @Email,
                    Phone    = @Phone,
                    [Role]   = @Role,
                    [Status] = @Status
                WHERE UserId = @UserId;

                IF EXISTS (SELECT 1 FROM UserProfiles WHERE UserId = @UserId)
                    UPDATE UserProfiles
                    SET Gender = @Gender, Address = @Address, AvatarUrl = @AvatarUrl, UpdatedAt = GETDATE()
                    WHERE UserId = @UserId;
                ELSE
                    INSERT INTO UserProfiles (UserId, Gender, Address, AvatarUrl)
                    VALUES (@UserId, @Gender, @Address, @AvatarUrl);";

            using var conn = CreateConnection();
            await conn.ExecuteAsync(sql, user);
        }
        catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
        {
            if (ex.Message.Contains("UserName") || ex.Message.Contains("UQ__Users__C9F28456"))
                throw new Exception("This Username is already taken.");
            if (ex.Message.Contains("Email") || ex.Message.Contains("UQ__Users__A9D10534"))
                throw new Exception("This Email is already registered.");
            if (ex.Message.Contains("Phone") || ex.Message.Contains("UQ__Users__5C7E359F"))
                throw new Exception("This Phone number is already registered.");
                
            throw new Exception("A user with this unique information already exists.");
        }
    }

    // ─── TOGGLE STATUS ────────────────────────────────────────────────────────
    public async Task ToggleStatusAsync(int userId, bool newStatus)
    {
        const string sql = "UPDATE Users SET [Status] = @Status WHERE UserId = @UserId;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Status = newStatus, UserId = userId });
    }

    // ─── DELETE ───────────────────────────────────────────────────────────────
    public async Task DeleteUserAsync(int userId)
    {
        // Safe delete cascading:
        // Order: FavoriteRooms -> Messages -> Notifications -> Reports 
        // -> Payments -> Invoices -> MeterReadings -> UtilityRates -> Feedbacks -> RentalContracts -> RoomImages -> Rooms 
        // -> UserProfiles -> Users
        const string sql = @"
            BEGIN TRANSACTION;
            BEGIN TRY
                -- Delete from tables where User is explicitly involved
                DELETE FROM FavoriteRooms WHERE UserId = @UserId;
                DELETE FROM Messages WHERE SenderId = @UserId OR ReceiverId = @UserId;
                DELETE FROM Notifications WHERE UserId = @UserId;
                DELETE FROM Reports WHERE CreatedBy = @UserId;
                DELETE FROM Feedbacks WHERE UserId = @UserId;

                -- Delete Tenants Contracts and their Payments/Invoices
                DELETE p FROM Payments p INNER JOIN Invoices i ON p.InvoiceId = i.InvoiceId INNER JOIN RentalContracts c ON i.ContractId = c.ContractId WHERE c.TenantId = @UserId;
                DELETE i FROM Invoices i INNER JOIN RentalContracts c ON i.ContractId = c.ContractId WHERE c.TenantId = @UserId;
                DELETE FROM RentalContracts WHERE TenantId = @UserId;

                -- Delete Landlord's Rooms and their dependencies (Feedbacks, Contracts, Invoices, Payments, Images, Utils)
                -- 1. Get room IDs for this landlord
                SELECT RoomId INTO #TempRooms FROM Rooms WHERE LandlordId = @UserId;

                -- 2. Delete dependencies of those rooms
                DELETE p FROM Payments p INNER JOIN Invoices i ON p.InvoiceId = i.InvoiceId INNER JOIN RentalContracts c ON i.ContractId = c.ContractId WHERE c.RoomId IN (SELECT RoomId FROM #TempRooms);
                DELETE i FROM Invoices i INNER JOIN RentalContracts c ON i.ContractId = c.ContractId WHERE c.RoomId IN (SELECT RoomId FROM #TempRooms);
                DELETE FROM RentalContracts WHERE RoomId IN (SELECT RoomId FROM #TempRooms);
                DELETE FROM Feedbacks WHERE RoomId IN (SELECT RoomId FROM #TempRooms);
                DELETE FROM UtilityRates WHERE RoomId IN (SELECT RoomId FROM #TempRooms);
                DELETE FROM MeterReadings WHERE RoomId IN (SELECT RoomId FROM #TempRooms);
                DELETE FROM RoomImages WHERE RoomId IN (SELECT RoomId FROM #TempRooms);
                DELETE FROM FavoriteRooms WHERE RoomId IN (SELECT RoomId FROM #TempRooms);
                DELETE FROM Messages WHERE RoomId IN (SELECT RoomId FROM #TempRooms);

                -- 3. Delete the rooms
                DELETE FROM Rooms WHERE LandlordId = @UserId;
                DROP TABLE #TempRooms;

                -- Finally delete profile and user
                DELETE FROM UserProfiles WHERE UserId = @UserId;
                DELETE FROM Users WHERE UserId = @UserId;

                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        ";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { UserId = userId });
    }
}
