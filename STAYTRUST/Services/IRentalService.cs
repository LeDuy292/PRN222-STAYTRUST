using System.Collections.Generic;
using System.Threading.Tasks;
using STAYTRUST.Models;

namespace STAYTRUST.Services;

public interface IRentalService
{
    Task<List<RentalContract>> GetUserRentalsAsync(int userId);
    Task<RentalContract?> GetRentalContractByIdAsync(int contractId);
    Task<Room?> GetRoomByIdAsync(int roomId);
    Task<List<Invoice>> GetInvoicesByContractIdAsync(int contractId);
    Task<List<MeterReading>> GetMeterReadingsByRoomIdAsync(int roomId);
    Task<bool> HasActiveRentalAsync(int userId);
    Task<RentalContract?> CreateRentalContractAsync(int userId, int roomId);
    Task<bool> TerminateRentalContractAsync(int contractId);
    Task<bool> SubmitFeedbackAsync(Feedback feedback);
    Task<bool> CanFeedbackAsync(int userId, int roomId);
}
