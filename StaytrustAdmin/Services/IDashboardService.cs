using StaytrustAdmin.Models;

namespace StaytrustAdmin.Services;

public interface IDashboardService
{
    Task<DashboardKpi>              GetKpiAsync();
    Task<List<MonthlyPoint>>        GetMonthlyUsersAsync(int months = 12);
    Task<List<MonthlyPoint>>        GetMonthlyContractsAsync(int months = 12);
    Task<List<MonthlyPoint>>        GetMonthlyRevenueAsync(int months = 12);
    Task<List<RoomStatusCount>>     GetRoomsByStatusAsync();
    Task<List<RoomTypeCount>>       GetRoomsByTypeAsync();
    Task<List<TopRoom>>             GetTopViewedRoomsAsync(int top = 5);
    Task<List<TopRoom>>             GetTopFavoriteRoomsAsync(int top = 5);
    Task<FinanceSummary>            GetFinanceSummaryAsync();
    Task<List<TopLandlordRevenue>>  GetTopLandlordRevenueAsync(int top = 5);
    Task<List<UserRoleCount>>       GetUsersByRoleAsync();
    Task<List<TopLandlordRooms>>    GetTopLandlordRoomsAsync(int top = 5);
    Task<List<ExpiringContract>>    GetExpiringContractsAsync(int withinDays = 30);
    Task<List<OverdueInvoice>>      GetOverdueInvoicesAsync(int olderThanDays = 30);
    Task<List<PendingReport>>       GetPendingReportsAsync();
}
