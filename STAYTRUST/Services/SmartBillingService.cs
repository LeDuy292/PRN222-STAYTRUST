using Microsoft.EntityFrameworkCore;
using STAYTRUST.Data;
using STAYTRUST.Models;
using STAYTRUST.Models.DTOs;

namespace STAYTRUST.Services;

public interface ISmartBillingService
{
    Task<MeterReadingDto> SubmitMeterReadingAsync(int landlordId, CreateMeterReadingDto dto);
    Task<MeterReadingDto> GetMeterReadingAsync(int readingId, int landlordId);
    Task<List<MeterReadingDto>> GetMeterReadingsByRoomAsync(int roomId, int landlordId);
    Task<List<MeterReadingDto>> GetMeterReadingsByMonthAsync(int landlordId, string month);
    Task<bool> VerifyMeterReadingAsync(int readingId, int landlordId);
    Task<bool> DeleteMeterReadingAsync(int readingId, int landlordId);
    Task<InvoiceDto> GenerateInvoiceAsync(int landlordId, int contractId, string month);
    Task<List<InvoiceDto>> GenerateBulkInvoicesAsync(int landlordId, BulkGenerateInvoicesDto dto);
    Task<InvoiceDto> GetInvoiceAsync(int invoiceId, int landlordId);
    Task<List<InvoiceDto>> GetInvoicesByMonthAsync(int landlordId, string month);
    Task<List<InvoiceDto>> GetUnpaidInvoicesAsync(int landlordId);
    Task<byte[]> ExportInvoicePdfAsync(int invoiceId, int landlordId);
    Task<byte[]> ExportBulkInvoicesPdfAsync(int landlordId, string month);
}

public class SmartBillingService : ISmartBillingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SmartBillingService> _logger;
    private readonly INotificationService _notificationService;

    public SmartBillingService(AppDbContext context, ILogger<SmartBillingService> logger, INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<MeterReadingDto> SubmitMeterReadingAsync(int landlordId, CreateMeterReadingDto dto)
    {
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == dto.RoomId && r.LandlordId == landlordId);
        if (room == null) throw new UnauthorizedAccessException("You don't have permission to this room.");

        var existing = await _context.MeterReadings.FirstOrDefaultAsync(m => m.RoomId == dto.RoomId && m.Month == dto.Month);
        if (existing != null) throw new InvalidOperationException($"Meter reading already exists for {dto.Month}");

        if (dto.ElectricNew < dto.ElectricOld) throw new InvalidOperationException("Electric new reading cannot be less than old reading");
        if (dto.WaterNew < dto.WaterOld) throw new InvalidOperationException("Water new reading cannot be less than old reading");

        var reading = new MeterReading
        {
            RoomId = dto.RoomId,
            Month = dto.Month,
            ElectricOld = dto.ElectricOld,
            ElectricNew = dto.ElectricNew,
            WaterOld = dto.WaterOld,
            WaterNew = dto.WaterNew,
            Status = "Submitted",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.MeterReadings.Add(reading);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Meter reading submitted for room {dto.RoomId} month {dto.Month}");
        return MapToDto(reading, room.Title);
    }

    public async Task<MeterReadingDto> GetMeterReadingAsync(int readingId, int landlordId)
    {
        var reading = await _context.MeterReadings.Include(m => m.Room)
            .FirstOrDefaultAsync(m => m.ReadingId == readingId && m.Room.LandlordId == landlordId);
        if (reading == null) throw new UnauthorizedAccessException("Meter reading not found");
        return MapToDto(reading, reading.Room.Title);
    }

    public async Task<List<MeterReadingDto>> GetMeterReadingsByRoomAsync(int roomId, int landlordId)
    {
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId && r.LandlordId == landlordId);
        if (room == null) throw new UnauthorizedAccessException("Room not found");

        var readings = await _context.MeterReadings.Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.Month).ToListAsync();
        return readings.Select(r => MapToDto(r, room.Title)).ToList();
    }

    public async Task<List<MeterReadingDto>> GetMeterReadingsByMonthAsync(int landlordId, string month)
    {
        var readings = await _context.MeterReadings.Include(m => m.Room)
            .Where(m => m.Month == month && m.Room.LandlordId == landlordId)
            .OrderBy(m => m.RoomId).ToListAsync();
        return readings.Select(r => MapToDto(r, r.Room.Title)).ToList();
    }

    public async Task<bool> VerifyMeterReadingAsync(int readingId, int landlordId)
    {
        var reading = await _context.MeterReadings.Include(m => m.Room)
            .FirstOrDefaultAsync(m => m.ReadingId == readingId && m.Room.LandlordId == landlordId);
        if (reading == null) throw new UnauthorizedAccessException("Meter reading not found");

        reading.Status = "Verified";
        reading.UpdatedAt = DateTime.Now;
        _context.MeterReadings.Update(reading);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMeterReadingAsync(int readingId, int landlordId)
    {
        var reading = await _context.MeterReadings.Include(m => m.Room)
            .FirstOrDefaultAsync(m => m.ReadingId == readingId && m.Room.LandlordId == landlordId);
        if (reading == null) throw new UnauthorizedAccessException("Meter reading not found");

        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Month == reading.Month);
        if (invoice != null) throw new InvalidOperationException("Cannot delete meter reading that has been invoiced");

        _context.MeterReadings.Remove(reading);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<InvoiceDto> GenerateInvoiceAsync(int landlordId, int contractId, string month)
    {
        var contract = await _context.RentalContracts.Include(c => c.Room).Include(c => c.Tenant)
            .FirstOrDefaultAsync(c => c.ContractId == contractId && c.Room.LandlordId == landlordId);
        if (contract == null) throw new UnauthorizedAccessException("Contract not found");

        var existing = await _context.Invoices.FirstOrDefaultAsync(i => i.ContractId == contractId && i.Month == month);
        if (existing != null) throw new InvalidOperationException($"Invoice already exists for {month}");

        var meterReading = await _context.MeterReadings.FirstOrDefaultAsync(m => m.RoomId == contract.RoomId && m.Month == month);
        if (meterReading == null) throw new InvalidOperationException("Meter reading not found for this month");

        var rates = await _context.UtilityRates.FirstOrDefaultAsync(r => r.RoomId == contract.RoomId);
        if (rates == null)
        {
            rates = new UtilityRate { RoomId = contract.RoomId, ElectricPrice = 3500, WaterPrice = 12000 };
            _context.UtilityRates.Add(rates);
            await _context.SaveChangesAsync();
        }

        int electricUsage = (meterReading.ElectricNew ?? 0) - (meterReading.ElectricOld ?? 0);
        int waterUsage = (meterReading.WaterNew ?? 0) - (meterReading.WaterOld ?? 0);
        decimal electricFee = electricUsage * rates.ElectricPrice;
        decimal waterFee = waterUsage * rates.WaterPrice;

        var invoice = new Invoice
        {
            ContractId = contractId,
            Month = month,
            RoomPrice = contract.Room.Price,
            ElectricFee = electricFee,
            WaterFee = waterFee,
            Status = "Unpaid",
            CreatedAt = DateTime.Now
        };

        _context.Invoices.Add(invoice);
        meterReading.Status = "Invoiced";
        meterReading.UpdatedAt = DateTime.Now;
        _context.MeterReadings.Update(meterReading);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Invoice generated for contract {contractId} month {month}");
        await _notificationService.SendNotificationAsync(contract.TenantId, "Hóa ??n m?i",
            $"Hóa ??n tháng {month} cho phòng {contract.Room.Title} ?ã ???c t?o.");

        return MapToInvoiceDto(invoice, contract, meterReading, rates);
    }

    public async Task<List<InvoiceDto>> GenerateBulkInvoicesAsync(int landlordId, BulkGenerateInvoicesDto dto)
    {
        var invoices = new List<InvoiceDto>();
        foreach (var roomId in dto.RoomIds)
        {
            try
            {
                var contract = await _context.RentalContracts.FirstOrDefaultAsync(c => c.RoomId == roomId && c.Status == "Active");
                if (contract == null) continue;
                var invoice = await GenerateInvoiceAsync(landlordId, contract.ContractId, dto.Month);
                invoices.Add(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to generate invoice for room {roomId}: {ex.Message}");
            }
        }
        return invoices;
    }

    public async Task<InvoiceDto> GetInvoiceAsync(int invoiceId, int landlordId)
    {
        var invoice = await _context.Invoices.Include(i => i.Contract).ThenInclude(c => c.Room)
            .ThenInclude(r => r.Landlord).Include(i => i.Contract.Tenant)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.Contract.Room.LandlordId == landlordId);
        if (invoice == null) throw new UnauthorizedAccessException("Invoice not found");

        var meterReading = await _context.MeterReadings.FirstOrDefaultAsync(m => m.RoomId == invoice.Contract.RoomId && m.Month == invoice.Month);
        var rates = await _context.UtilityRates.FirstOrDefaultAsync(r => r.RoomId == invoice.Contract.RoomId);
        return MapToInvoiceDto(invoice, invoice.Contract, meterReading, rates);
    }

    public async Task<List<InvoiceDto>> GetInvoicesByMonthAsync(int landlordId, string month)
    {
        var invoices = await _context.Invoices.Include(i => i.Contract)
            .ThenInclude(c => c.Room).ThenInclude(r => r.Landlord)
            .Include(i => i.Contract.Tenant)
            .Where(i => i.Month == month && i.Contract.Room.LandlordId == landlordId)
            .OrderBy(i => i.Contract.RoomId).ToListAsync();

        var result = new List<InvoiceDto>();
        foreach (var invoice in invoices)
        {
            var meterReading = await _context.MeterReadings.FirstOrDefaultAsync(m => m.RoomId == invoice.Contract.RoomId && m.Month == invoice.Month);
            var rates = await _context.UtilityRates.FirstOrDefaultAsync(r => r.RoomId == invoice.Contract.RoomId);
            result.Add(MapToInvoiceDto(invoice, invoice.Contract, meterReading, rates));
        }
        return result;
    }

    public async Task<List<InvoiceDto>> GetUnpaidInvoicesAsync(int landlordId)
    {
        var invoices = await _context.Invoices.Include(i => i.Contract)
            .ThenInclude(c => c.Room).ThenInclude(r => r.Landlord)
            .Include(i => i.Contract.Tenant)
            .Where(i => i.Status == "Unpaid" && i.Contract.Room.LandlordId == landlordId)
            .OrderBy(i => i.Month).ToListAsync();

        var result = new List<InvoiceDto>();
        foreach (var invoice in invoices)
        {
            var meterReading = await _context.MeterReadings.FirstOrDefaultAsync(m => m.RoomId == invoice.Contract.RoomId && m.Month == invoice.Month);
            var rates = await _context.UtilityRates.FirstOrDefaultAsync(r => r.RoomId == invoice.Contract.RoomId);
            result.Add(MapToInvoiceDto(invoice, invoice.Contract, meterReading, rates));
        }
        return result;
    }

    public async Task<byte[]> ExportInvoicePdfAsync(int invoiceId, int landlordId)
    {
        await GetInvoiceAsync(invoiceId, landlordId);
        return new byte[] { };
    }

    public async Task<byte[]> ExportBulkInvoicesPdfAsync(int landlordId, string month)
    {
        await GetInvoicesByMonthAsync(landlordId, month);
        return new byte[] { };
    }

    private MeterReadingDto MapToDto(MeterReading reading, string roomTitle) => new()
    {
        ReadingId = reading.ReadingId,
        RoomId = reading.RoomId,
        RoomTitle = roomTitle,
        Month = reading.Month,
        ElectricOld = reading.ElectricOld ?? 0,
        ElectricNew = reading.ElectricNew ?? 0,
        WaterOld = reading.WaterOld ?? 0,
        WaterNew = reading.WaterNew ?? 0,
        Status = reading.Status,
        CreatedAt = reading.CreatedAt ?? DateTime.Now,
        UpdatedAt = reading.UpdatedAt ?? DateTime.Now
    };

    private InvoiceDto MapToInvoiceDto(Invoice invoice, RentalContract contract, MeterReading meterReading, UtilityRate rates)
    {
        var electricUsage = (meterReading?.ElectricNew ?? 0) - (meterReading?.ElectricOld ?? 0);
        var waterUsage = (meterReading?.WaterNew ?? 0) - (meterReading?.WaterOld ?? 0);

        return new InvoiceDto
        {
            InvoiceId = invoice.InvoiceId,
            ContractId = invoice.ContractId,
            RoomId = contract.RoomId,
            RoomTitle = contract.Room.Title,
            TenantName = contract.Tenant.FullName,
            TenantEmail = contract.Tenant.Email,
            Month = invoice.Month,
            RoomPrice = invoice.RoomPrice,
            ElectricFee = invoice.ElectricFee,
            WaterFee = invoice.WaterFee,
            TotalAmount = invoice.RoomPrice + invoice.ElectricFee + invoice.WaterFee,
            ElectricUsage = (int)electricUsage,
            WaterUsage = (int)waterUsage,
            ElectricPrice = rates?.ElectricPrice ?? 3500,
            WaterPrice = rates?.WaterPrice ?? 12000,
            Status = invoice.Status,
            CreatedAt = invoice.CreatedAt
        };
    }
}
