using StaytrustAdmin.Models;

namespace StaytrustAdmin.Services;

public interface IInvoiceManagementService
{
    Task<List<LandlordInvoiceSummary>> GetLandlordsAsync(string? search);
    Task<List<RoomInvoiceSummary>> GetRoomsByLandlordAsync(int landlordId);
    Task<List<RoomInvoiceDetail>> GetInvoicesByRoomAsync(int roomId);
}
