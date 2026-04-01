using Microsoft.EntityFrameworkCore;
using STAYTRUST.Data;
using STAYTRUST.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace STAYTRUST.Services;

public class ReportService : IReportService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ReportService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<Report>> GetUserReportsAsync(int userId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Reports
            .Where(r => r.CreatedBy == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task CreateReportAsync(Report report)
    {
        report.CreatedAt = DateTime.Now;
        if (string.IsNullOrEmpty(report.Status))
        {
            report.Status = "Pending";
        }
        
        using var context = await _factory.CreateDbContextAsync();
        context.Reports.Add(report);
        await context.SaveChangesAsync();
    }

    public async Task<List<Report>> GetLandlordReportsAsync(int landlordId)
    {
        using var context = await _factory.CreateDbContextAsync();
        // A report belongs to a room, and a room belongs to a landlord.
        return await context.Reports
            .Include(r => r.Room)
            .Include(r => r.CreatedByNavigation)
            .Where(r => r.Room != null && r.Room.LandlordId == landlordId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateReportStatusAsync(int reportId, string status)
    {
        using var context = await _factory.CreateDbContextAsync();
        var report = await context.Reports.FindAsync(reportId);
        if (report != null)
        {
            report.Status = status;
            await context.SaveChangesAsync();
        }
    }

    public async Task<Report?> GetReportByIdAsync(int reportId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Reports
            .Include(r => r.Room)
            .Include(r => r.CreatedByNavigation)
            .FirstOrDefaultAsync(r => r.ReportId == reportId);
    }
}
