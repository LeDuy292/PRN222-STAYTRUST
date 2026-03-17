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
    private readonly AppDbContext _context;

    public RentalService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RentalContract>> GetUserRentalsAsync(int userId)
    {
        var rentals = await _context.RentalContracts
            .Include(r => r.Room)
                .ThenInclude(rm => rm.RoomImages)
            .Include(r => r.Room)
                .ThenInclude(rm => rm.Landlord) // Changed from User to Landlord
            .Where(r => r.TenantId == userId)
            .ToListAsync();

        return rentals;
    }

    public async Task<Room?> GetRoomByIdAsync(int roomId)
    {
        return await _context.Rooms
            .Include(r => r.RoomImages)
            .Include(r => r.Landlord) // Changed from User to Landlord
            .FirstOrDefaultAsync(r => r.RoomId == roomId);
    }

    public async Task<List<Invoice>> GetInvoicesByContractIdAsync(int contractId)
    {
        var invoices = await _context.Invoices
            .Where(i => i.ContractId == contractId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invoices;
    }

    public async Task<List<MeterReading>> GetMeterReadingsByRoomIdAsync(int roomId)
    {
        var readings = await _context.MeterReadings
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return readings;
    }

    public async Task<bool> HasActiveRentalAsync(int userId)
    {
        return await _context.RentalContracts
            .AnyAsync(r => r.TenantId == userId && r.Status == "Active");
    }

    public async Task<RentalContract?> CreateRentalContractAsync(int userId, int roomId)
    {
        // 1. Check if user already has an active rental
        if (await HasActiveRentalAsync(userId))
        {
            return null; // Logic check failed: already has active room
        }

        // 2. Create the contract
        var room = await GetRoomByIdAsync(roomId);
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

        _context.RentalContracts.Add(contract);
        room.Status = "Rented";
        
        await _context.SaveChangesAsync();
        return contract;
    }

    public async Task<bool> TerminateRentalContractAsync(int contractId)
    {
        var contract = await _context.RentalContracts
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

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SubmitFeedbackAsync(Feedback feedback)
    {
        if (feedback == null) return false;
        
        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();
        return true;
    }
}
