using StaytrustAdmin.Models;

namespace StaytrustAdmin.Services;

public interface IListingService
{
    // ─── Landlord View ────────────────────────────────────────────────────────
    Task<List<LandlordSummary>> GetLandlordsAsync(string? search);
    Task BlockLandlordAsync(int userId, bool block);

    // ─── Room / Listing View ──────────────────────────────────────────────────
    Task<(List<Room> Rooms, int TotalCount)> GetRoomsByLandlordAsync(int landlordId, string? status, int page, int pageSize);
    Task<(List<Room> Rooms, int TotalCount)> GetListingsAsync(string? search, string? status, int page, int pageSize);
    Task<(int Total, int Active, int Hidden, int SpamFlagged)> GetListingStatsAsync();
    Task UpdateListingStatusAsync(int roomId, string newStatus);
    Task DeleteListingAsync(int roomId);
}
