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
}
