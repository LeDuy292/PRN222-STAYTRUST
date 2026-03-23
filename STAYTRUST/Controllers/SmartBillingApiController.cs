using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STAYTRUST.Data;
using STAYTRUST.Models;
using STAYTRUST.Models.DTOs;
using STAYTRUST.Services;
using System.Security.Claims;

namespace STAYTRUST.Controllers
{
    [ApiController]
    [Route("api/smart-billing")]
    [Authorize]
    public class SmartBillingApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SmartBillingApiController> _logger;

        public SmartBillingApiController(
            AppDbContext context,
            INotificationService notificationService,
            ILogger<SmartBillingApiController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim?.Value, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get the last meter reading for a property
        /// </summary>
        [HttpGet("last-reading/{roomId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLastMeterReading(int roomId)
        {
            try
            {
                var lastReading = await _context.MeterReadings
                    .Where(m => m.RoomId == roomId)
                    .OrderByDescending(m => m.ReadingId)
                    .FirstOrDefaultAsync();

                if (lastReading == null)
                {
                    // Return default values if no previous reading
                    return Ok(new
                    {
                        data = new
                        {
                            readingId = 0,
                            roomId = roomId,
                            month = "",
                            electricOld = 0,
                            electricNew = 0,
                            waterOld = 0,
                            waterNew = 0
                        }
                    });
                }

                return Ok(new
                {
                    data = new
                    {
                        lastReading.ReadingId,
                        lastReading.RoomId,
                        lastReading.Month,
                        ElectricOld = lastReading.ElectricOld,
                        ElectricNew = lastReading.ElectricNew,
                        WaterOld = lastReading.WaterOld,
                        WaterNew = lastReading.WaterNew
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting last meter reading: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Create invoice for a property
        /// </summary>
        [HttpPost("create-invoice")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceApiDto dto)
        {
            try
            {
                // Validate room exists and user is landlord
                var room = await _context.Rooms.FindAsync(dto.RoomId);
                if (room == null)
                    return NotFound(new { message = "Property not found" });

                // Check if user is authenticated and is the landlord
                var userId = GetUserId();
                if (userId != 0 && room.LandlordId != userId)
                    return Forbid();

                // Check if tenant exists (room must be occupied)
                var contract = await _context.RentalContracts
                    .Where(c => c.RoomId == dto.RoomId && c.Status == "Active")
                    .FirstOrDefaultAsync();

                if (contract == null)
                    return BadRequest(new { message = "No active rental contract for this property" });

                // Check if invoice for this month already exists
                var existingInvoice = await _context.Invoices
                    .Where(i => i.ContractId == contract.ContractId && i.Month == dto.Month)
                    .FirstOrDefaultAsync();

                if (existingInvoice != null)
                    return BadRequest(new { message = "Invoice for this month already exists" });

                // Create meter reading
                var meterReading = new MeterReading
                {
                    RoomId = dto.RoomId,
                    Month = dto.Month,
                    ElectricOld = dto.ElectricOld,
                    ElectricNew = dto.ElectricNew,
                    WaterOld = dto.WaterOld,
                    WaterNew = dto.WaterNew,
                    CreatedAt = DateTime.Now
                };

                // Calculate fees
                long electricFee = (long)(dto.ElectricNew - dto.ElectricOld) * dto.ElectricPrice;
                long waterFee = (long)(dto.WaterNew - dto.WaterOld) * dto.WaterPrice;

                // Create invoice
                var invoice = new Invoice
                {
                    ContractId = contract.ContractId,
                    Month = dto.Month,
                    RoomPrice = dto.RoomPrice,
                    ElectricFee = electricFee,
                    WaterFee = waterFee,
                    Status = "Unpaid",
                    CreatedAt = DateTime.Now
                };

                _context.MeterReadings.Add(meterReading);
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Send notification to tenant
                var tenant = await _context.Users.FindAsync(contract.TenantId);
                if (tenant != null)
                {
                    var totalAmount = invoice.RoomPrice + invoice.ElectricFee + invoice.WaterFee + (long)dto.ServicesTotal;
                    
                    await _notificationService.SendNotificationAsync(
                        contract.TenantId,
                        "New Monthly Invoice",
                        $"Your monthly invoice for {room.Title} is ready. Total: ?{totalAmount:N0}"
                    );

                    // TODO: Send email notification
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        invoice.ContractId,
                        invoice.Month,
                        invoice.RoomPrice,
                        invoice.ElectricFee,
                        invoice.WaterFee,
                        TotalAmount = invoice.RoomPrice + invoice.ElectricFee + invoice.WaterFee + (long)dto.ServicesTotal,
                        invoice.Status
                    },
                    message = "Invoice created and sent to tenant"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating invoice: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get invoices for a property
        /// </summary>
        [HttpGet("invoices/{roomId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoomInvoices(int roomId)
        {
            try
            {
                var invoices = await _context.Invoices
                    .Where(i => i.Contract.RoomId == roomId)
                    .OrderByDescending(i => i.Month)
                    .Select(i => new
                    {
                        i.ContractId,
                        i.Month,
                        i.RoomPrice,
                        i.ElectricFee,
                        i.WaterFee,
                        TotalAmount = i.RoomPrice + i.ElectricFee + i.WaterFee,
                        i.Status,
                        i.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new { data = invoices });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting invoices: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get invoice details
        /// </summary>
        [HttpGet("invoice/{invoiceId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInvoiceDetails(int invoiceId)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Contract)
                    .ThenInclude(c => c.Room)
                    .FirstOrDefaultAsync(i => i.ContractId == invoiceId);

                if (invoice == null)
                    return NotFound(new { message = "Invoice not found" });

                return Ok(new
                {
                    data = new
                    {
                        invoice.ContractId,
                        invoice.Month,
                        invoice.RoomPrice,
                        invoice.ElectricFee,
                        invoice.WaterFee,
                        TotalAmount = invoice.RoomPrice + invoice.ElectricFee + invoice.WaterFee,
                        invoice.Status,
                        invoice.CreatedAt,
                        Room = new
                        {
                            invoice.Contract.Room.RoomId,
                            invoice.Contract.Room.Title,
                            invoice.Contract.Room.Address
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting invoice details: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }

    public class CreateInvoiceApiDto
    {
        public int RoomId { get; set; }
        public string Month { get; set; } = "";
        public int ElectricOld { get; set; }
        public int ElectricNew { get; set; }
        public int WaterOld { get; set; }
        public int WaterNew { get; set; }
        public int ElectricPrice { get; set; } = 3500;
        public int WaterPrice { get; set; } = 12000;
        public long RoomPrice { get; set; }
        public long ServicesTotal { get; set; }
    }
}
