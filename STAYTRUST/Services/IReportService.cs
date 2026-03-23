using System.Collections.Generic;
using System.Threading.Tasks;
using STAYTRUST.Models;

namespace STAYTRUST.Services;

public interface IReportService
{
    Task<List<Report>> GetUserReportsAsync(int userId);
    Task CreateReportAsync(Report report);
}
