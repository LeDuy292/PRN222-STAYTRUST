using Dapper;
using Microsoft.Data.SqlClient;
using StaytrustAdmin.Models;

namespace StaytrustAdmin.Services;

public class DashboardService : IDashboardService
{
    private readonly string _cs;
    public DashboardService(IConfiguration cfg) =>
        _cs = cfg.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found.");

    private SqlConnection Conn() => new SqlConnection(_cs);

    // ─── KPI ──────────────────────────────────────────────────────────────────
    public async Task<DashboardKpi> GetKpiAsync()
    {
        const string sql = @"
            DECLARE @ThisMonthStart DATE = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);
            DECLARE @LastMonthStart DATE = DATEADD(MONTH,-1,@ThisMonthStart);
            DECLARE @LastMonthEnd   DATE = DATEADD(DAY,-1,@ThisMonthStart);

            SELECT
                (SELECT COUNT(*) FROM Users)                                                            AS TotalUsers,
                (SELECT COUNT(*) FROM Rooms WHERE Status IN ('Active','Available'))                     AS TotalActiveRooms,
                (SELECT COUNT(*) FROM RentalContracts WHERE Status = 'Active')                          AS TotalActiveContracts,
                (SELECT ISNULL(SUM(Amount),0) FROM Payments WHERE Status='Success' AND CAST(PaymentDate AS DATE) >= @ThisMonthStart) AS RevenueThisMonth,
                (SELECT ISNULL(SUM(Amount),0) FROM Payments WHERE Status='Success' AND CAST(PaymentDate AS DATE) BETWEEN @LastMonthStart AND @LastMonthEnd) AS RevenueLastMonth,
                (SELECT COUNT(*) FROM Users   WHERE CAST(CreatedAt AS DATE) >= @ThisMonthStart) AS NewUsersThisMonth,
                (SELECT COUNT(*) FROM Rooms   WHERE CAST(CreatedAt AS DATE) >= @ThisMonthStart) AS NewRoomsThisMonth,
                (SELECT COUNT(*) FROM RentalContracts WHERE CAST(CreatedAt AS DATE) >= @ThisMonthStart) AS NewContractsThisMonth;";

        using var conn = Conn();
        return await conn.QueryFirstAsync<DashboardKpi>(sql);
    }

    // ─── MONTHLY GROWTH ───────────────────────────────────────────────────────
    public async Task<List<MonthlyPoint>> GetMonthlyUsersAsync(int months = 12)
    {
        var sql = $@"
            SELECT FORMAT(CreatedAt,'yyyy-MM') AS Month, COUNT(*) AS Count
            FROM Users
            WHERE CreatedAt >= DATEADD(MONTH,-{months},GETDATE())
            GROUP BY FORMAT(CreatedAt,'yyyy-MM')
            ORDER BY Month;";
        using var conn = Conn();
        return (await conn.QueryAsync<MonthlyPoint>(sql)).ToList();
    }

    public async Task<List<MonthlyPoint>> GetMonthlyContractsAsync(int months = 12)
    {
        var sql = $@"
            SELECT FORMAT(CreatedAt,'yyyy-MM') AS Month, COUNT(*) AS Count
            FROM RentalContracts
            WHERE CreatedAt >= DATEADD(MONTH,-{months},GETDATE())
            GROUP BY FORMAT(CreatedAt,'yyyy-MM')
            ORDER BY Month;";
        using var conn = Conn();
        return (await conn.QueryAsync<MonthlyPoint>(sql)).ToList();
    }

    public async Task<List<MonthlyPoint>> GetMonthlyRevenueAsync(int months = 12)
    {
        var sql = $@"
            SELECT FORMAT(PaymentDate,'yyyy-MM') AS Month, SUM(Amount) AS Amount
            FROM Payments
            WHERE Status='Success' AND PaymentDate >= DATEADD(MONTH,-{months},GETDATE())
            GROUP BY FORMAT(PaymentDate,'yyyy-MM')
            ORDER BY Month;";
        using var conn = Conn();
        return (await conn.QueryAsync<MonthlyPoint>(sql)).ToList();
    }

    // ─── ROOM STATS ───────────────────────────────────────────────────────────
    public async Task<List<RoomStatusCount>> GetRoomsByStatusAsync()
    {
        const string sql = @"
            SELECT Status, COUNT(*) AS Count FROM Rooms
            GROUP BY Status ORDER BY Count DESC;";
        using var conn = Conn();
        return (await conn.QueryAsync<RoomStatusCount>(sql)).ToList();
    }

    public async Task<List<RoomTypeCount>> GetRoomsByTypeAsync()
    {
        const string sql = @"
            SELECT ISNULL([Type],'Other') AS Type, COUNT(*) AS Count FROM Rooms
            GROUP BY [Type] ORDER BY Count DESC;";
        using var conn = Conn();
        return (await conn.QueryAsync<RoomTypeCount>(sql)).ToList();
    }

    public async Task<List<TopRoom>> GetTopViewedRoomsAsync(int top = 5)
    {
        var sql = $@"
            SELECT TOP {top} r.RoomId, r.Title, u.FullName AS LandlordName, r.Views, r.Rating,
                   (SELECT COUNT(*) FROM FavoriteRooms f WHERE f.RoomId = r.RoomId) AS FavoriteCount
            FROM Rooms r LEFT JOIN Users u ON u.UserId = r.LandlordId
            ORDER BY r.Views DESC;";
        using var conn = Conn();
        return (await conn.QueryAsync<TopRoom>(sql)).ToList();
    }

    public async Task<List<TopRoom>> GetTopFavoriteRoomsAsync(int top = 5)
    {
        var sql = $@"
            SELECT TOP {top} r.RoomId, r.Title, u.FullName AS LandlordName, r.Views, r.Rating,
                   COUNT(f.FavoriteId) AS FavoriteCount
            FROM Rooms r
            LEFT JOIN Users u ON u.UserId = r.LandlordId
            LEFT JOIN FavoriteRooms f ON f.RoomId = r.RoomId
            GROUP BY r.RoomId, r.Title, u.FullName, r.Views, r.Rating
            ORDER BY FavoriteCount DESC;";
        using var conn = Conn();
        return (await conn.QueryAsync<TopRoom>(sql)).ToList();
    }

    // ─── FINANCE ──────────────────────────────────────────────────────────────
    public async Task<FinanceSummary> GetFinanceSummaryAsync()
    {
        const string sql = @"
            SELECT
                ISNULL(SUM(CASE WHEN i.Status='Paid'   THEN i.TotalAmount ELSE 0 END),0) AS TotalRevenuePaid,
                ISNULL(SUM(CASE WHEN i.Status='Unpaid' THEN i.TotalAmount ELSE 0 END),0) AS TotalUnpaid,
                SUM(CASE WHEN i.Status='Paid'   THEN 1 ELSE 0 END) AS PaidInvoices,
                SUM(CASE WHEN i.Status='Unpaid' THEN 1 ELSE 0 END) AS UnpaidInvoices,
                (SELECT COUNT(*) FROM Payments WHERE Status='Success') AS SuccessPayments,
                (SELECT COUNT(*) FROM Payments WHERE Status='Failed')  AS FailedPayments
            FROM Invoices i;";
        using var conn = Conn();
        return await conn.QueryFirstAsync<FinanceSummary>(sql);
    }

    public async Task<List<TopLandlordRevenue>> GetTopLandlordRevenueAsync(int top = 5)
    {
        var sql = $@"
            SELECT TOP {top}
                u.UserId, u.FullName,
                ISNULL(SUM(p.Amount),0) AS TotalRevenue,
                COUNT(DISTINCT r.RoomId) AS RoomCount
            FROM Users u
            LEFT JOIN Rooms r ON r.LandlordId = u.UserId
            LEFT JOIN RentalContracts c ON c.RoomId = r.RoomId
            LEFT JOIN Invoices i ON i.ContractId = c.ContractId
            LEFT JOIN Payments p ON p.InvoiceId = i.InvoiceId AND p.Status = 'Success'
            WHERE u.[Role] = 'Landlord'
            GROUP BY u.UserId, u.FullName
            ORDER BY TotalRevenue DESC;";
        using var conn = Conn();
        return (await conn.QueryAsync<TopLandlordRevenue>(sql)).ToList();
    }

    // ─── USER STATS ───────────────────────────────────────────────────────────
    public async Task<List<UserRoleCount>> GetUsersByRoleAsync()
    {
        const string sql = @"
            SELECT [Role], COUNT(*) AS Count FROM Users
            WHERE [Role] IS NOT NULL
            GROUP BY [Role];";
        using var conn = Conn();
        return (await conn.QueryAsync<UserRoleCount>(sql)).ToList();
    }

    public async Task<List<TopLandlordRooms>> GetTopLandlordRoomsAsync(int top = 5)
    {
        var sql = $@"
            SELECT TOP {top} u.UserId, u.FullName, u.Email, COUNT(r.RoomId) AS RoomCount
            FROM Users u
            LEFT JOIN Rooms r ON r.LandlordId = u.UserId
            WHERE u.[Role] = 'Landlord'
            GROUP BY u.UserId, u.FullName, u.Email
            ORDER BY RoomCount DESC;";
        using var conn = Conn();
        return (await conn.QueryAsync<TopLandlordRooms>(sql)).ToList();
    }

    // ─── ACTIVITY ALERTS ──────────────────────────────────────────────────────
    public async Task<List<ExpiringContract>> GetExpiringContractsAsync(int withinDays = 30)
    {
        var sql = $@"
            SELECT TOP 10
                c.ContractId,
                ut.FullName AS TenantName,
                r.Title AS RoomTitle,
                c.EndDate,
                DATEDIFF(DAY, GETDATE(), c.EndDate) AS DaysLeft
            FROM RentalContracts c
            JOIN Users ut ON ut.UserId = c.TenantId
            JOIN Rooms r  ON r.RoomId  = c.RoomId
            WHERE c.Status = 'Active'
              AND c.EndDate IS NOT NULL
              AND c.EndDate BETWEEN GETDATE() AND DATEADD(DAY,{withinDays},GETDATE())
            ORDER BY c.EndDate ASC;";
        using var conn = Conn();
        return (await conn.QueryAsync<ExpiringContract>(sql)).ToList();
    }

    public async Task<List<OverdueInvoice>> GetOverdueInvoicesAsync(int olderThanDays = 30)
    {
        var sql = $@"
            SELECT TOP 10
                i.InvoiceId, i.Month, i.TotalAmount,
                u.FullName AS TenantName,
                r.Title AS RoomTitle
            FROM Invoices i
            JOIN RentalContracts c ON c.ContractId = i.ContractId
            JOIN Users u ON u.UserId = c.TenantId
            JOIN Rooms r ON r.RoomId = c.RoomId
            WHERE i.Status = 'Unpaid'
              AND i.CreatedAt < DATEADD(DAY,-{olderThanDays},GETDATE())
            ORDER BY i.CreatedAt ASC;";
        using var conn = Conn();
        return (await conn.QueryAsync<OverdueInvoice>(sql)).ToList();
    }

    public async Task<List<PendingReport>> GetPendingReportsAsync()
    {
        const string sql = @"
            SELECT TOP 10 rp.ReportId, rp.ReportType, rp.Description, rp.CreatedAt,
                   u.FullName AS CreatedBy
            FROM Reports rp
            JOIN Users u ON u.UserId = rp.CreatedBy
            WHERE rp.Status = 'Pending'
            ORDER BY rp.CreatedAt DESC;";
        using var conn = Conn();
        return (await conn.QueryAsync<PendingReport>(sql)).ToList();
    }
}
