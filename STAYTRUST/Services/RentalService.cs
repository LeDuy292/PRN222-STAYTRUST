using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using STAYTRUST.Data;
using STAYTRUST.Models;

namespace STAYTRUST.Services;

public class RentalService : IRentalService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public RentalService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<RentalContract>> GetUserRentalsAsync(int userId)
    {
        using var context = await _factory.CreateDbContextAsync();
        var rentals = await context.RentalContracts
            .Include(r => r.Room)
                .ThenInclude(rm => rm.RoomImages)
            .Include(r => r.Room)
                .ThenInclude(rm => rm.Landlord) // Changed from User to Landlord
            .Where(r => r.TenantId == userId)
            .ToListAsync();

        return rentals;
    }

    public async Task<RentalContract?> GetRentalContractByIdAsync(int contractId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.RentalContracts
            .Include(r => r.Tenant)
            .Include(r => r.Room)
                .ThenInclude(rm => rm.Landlord)
            .FirstOrDefaultAsync(r => r.ContractId == contractId);
    }

    public async Task<Room?> GetRoomByIdAsync(int roomId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Rooms
            .Include(r => r.RoomImages)
            .Include(r => r.Landlord) // Changed from User to Landlord
            .FirstOrDefaultAsync(r => r.RoomId == roomId);
    }

    public async Task<List<Invoice>> GetInvoicesByContractIdAsync(int contractId)
    {
        using var context = await _factory.CreateDbContextAsync();
        var invoices = await context.Invoices
            .Where(i => i.ContractId == contractId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invoices;
    }

    public async Task<List<MeterReading>> GetMeterReadingsByRoomIdAsync(int roomId)
    {
        using var context = await _factory.CreateDbContextAsync();
        var readings = await context.MeterReadings
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return readings;
    }

    public async Task<bool> HasActiveRentalAsync(int userId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.RentalContracts
            .AnyAsync(r => r.TenantId == userId && r.Status == "Active");
    }

    public async Task<RentalContract?> CreateRentalContractAsync(int userId, int roomId)
    {
        using var context = await _factory.CreateDbContextAsync();
        
        // 1. Check if user already has an active rental
        if (await context.RentalContracts.AnyAsync(r => r.TenantId == userId && r.Status == "Active"))
        {
            return null; // Logic check failed: already has active room
        }

        // 2. Create the contract
        var room = await context.Rooms
            .Include(r => r.RoomImages)
            .Include(r => r.Landlord)
            .FirstOrDefaultAsync(r => r.RoomId == roomId);

        if (room == null || room.Status == "Rented")
        {
            return null;
        }

        var contract = new RentalContract
        {
            RoomId = roomId,
            TenantId = userId,
            StartDate = DateOnly.FromDateTime(DateTime.Now),
            Deposit = room.Price,
            Status = "Active"
        };

        context.RentalContracts.Add(contract);
        room.Status = "Rented";
        
        await context.SaveChangesAsync();
        return contract;
    }

    public async Task<bool> TerminateRentalContractAsync(int contractId)
    {
        using var context = await _factory.CreateDbContextAsync();
        var contract = await context.RentalContracts
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.ContractId == contractId);

        if (contract == null || contract.Status != "Active")
        {
            return false;
        }

        contract.Status = "Terminated";
        contract.EndDate = DateOnly.FromDateTime(DateTime.Now);
        
        if (contract.Room != null)
        {
            contract.Room.Status = "Available";
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SubmitFeedbackAsync(Feedback feedback)
    {
        if (feedback == null) return false;
        
        using var context = await _factory.CreateDbContextAsync();
        context.Feedbacks.Add(feedback);
        await context.SaveChangesAsync();
        return true;
    }
}
