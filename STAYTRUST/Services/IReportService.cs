using System.Collections.Generic;
using System.Threading.Tasks;
using STAYTRUST.Models;

namespace STAYTRUST.Services;

public interface IReportService
{
    Task<List<Report>> GetUserReportsAsync(int userId);
    Task CreateReportAsync(Report report);
    Task<List<Report>> GetLandlordReportsAsync(int landlordId);
    Task UpdateReportStatusAsync(int reportId, string status);
    Task<Report?> GetReportByIdAsync(int reportId);
}
