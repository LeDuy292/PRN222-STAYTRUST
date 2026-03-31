using Dapper;
using Microsoft.Data.SqlClient;
using StaytrustAdmin.Models;

namespace StaytrustAdmin.Services;

public class InvoiceManagementService : IInvoiceManagementService
{
    private readonly string _connectionString;

    public InvoiceManagementService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<List<LandlordInvoiceSummary>> GetLandlordsAsync(string? search)
    {
        var whereSearch = string.IsNullOrWhiteSpace(search)
            ? ""
            : "AND (u.FullName LIKE @Search OR u.Email LIKE @Search OR u.Phone LIKE @Search OR u.UserName LIKE @Search)";

        var sql = $@"
            SELECT 
                u.UserId,
                u.FullName,
                u.Email,
                u.Phone,
                u.Status AS IsActive,
                COUNT(r.RoomId) AS TotalRooms
            FROM Users u
            LEFT JOIN Rooms r ON u.UserId = r.LandlordId
            WHERE u.Role = 'Landlord' {whereSearch}
            GROUP BY u.UserId, u.FullName, u.Email, u.Phone, u.Status
            ORDER BY u.FullName ASC;";

        using var conn = CreateConnection();
        var result = await conn.QueryAsync<LandlordInvoiceSummary>(sql, new { Search = $"%{search}%" });
        return result.ToList();
    }

    public async Task<List<RoomInvoiceSummary>> GetRoomsByLandlordAsync(int landlordId)
    {
        var sql = @"
            SELECT
                r.RoomId,
                r.Title,
                r.[Type],
                r.Price,
                r.[Status],
                (SELECT COUNT(i.InvoiceId) 
                 FROM Invoices i 
                 JOIN RentalContracts rc ON i.ContractId = rc.ContractId 
                 WHERE rc.RoomId = r.RoomId) AS TotalInvoices
            FROM Rooms r
            WHERE r.LandlordId = @LandlordId
            ORDER BY r.Title ASC;";

        using var conn = CreateConnection();
        var result = await conn.QueryAsync<RoomInvoiceSummary>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<List<RoomInvoiceDetail>> GetInvoicesByRoomAsync(int roomId)
    {
        var sql = @"
            SELECT 
                i.InvoiceId,
                i.ContractId,
                i.[Month],
                i.RoomPrice,
                i.ElectricFee,
                i.WaterFee,
                (i.RoomPrice + i.ElectricFee + i.WaterFee) AS TotalAmount,
                i.[Status],
                i.CreatedAt,
                ut.FullName AS TenantName,
                ut.Phone AS TenantPhone
            FROM Invoices i
            JOIN RentalContracts rc ON i.ContractId = rc.ContractId
            JOIN Users ut ON rc.TenantId = ut.UserId
            WHERE rc.RoomId = @RoomId
            ORDER BY i.[Month] DESC;";

        using var conn = CreateConnection();
        var result = await conn.QueryAsync<RoomInvoiceDetail>(sql, new { RoomId = roomId });
        return result.ToList();
    }
}
