using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAYTRUST.Models.DTOs;
using STAYTRUST.Services;

namespace STAYTRUST.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Landlord")]
    public class SmartBillingController : ControllerBase
    {
        private readonly ISmartBillingService _billingService;
        private readonly ILogger<SmartBillingController> _logger;

        public SmartBillingController(ISmartBillingService billingService, ILogger<SmartBillingController> logger)
        {
            _billingService = billingService;
            _logger = logger;
        }

        #region Meter Readings

        [HttpPost("meter-readings/submit")]
        public async Task<IActionResult> SubmitMeterReading([FromBody] CreateMeterReadingDto dto)
        {
            try
            {
                var landlordId = GetLandlordId();
                var result = await _billingService.SubmitMeterReadingAsync(landlordId, dto);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error submitting meter reading: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("meter-readings/room/{roomId}")]
        public async Task<IActionResult> GetMeterReadingsByRoom(int roomId)
        {
            try
            {
                var landlordId = GetLandlordId();
                var result = await _billingService.GetMeterReadingsByRoomAsync(roomId, landlordId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("meter-readings/month/{month}")]
        public async Task<IActionResult> GetMeterReadingsByMonth(string month)
        {
            try
            {
                var landlordId = GetLandlordId();
                var result = await _billingService.GetMeterReadingsByMonthAsync(landlordId, month);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("meter-readings/{readingId}/verify")]
        public async Task<IActionResult> VerifyMeterReading(int readingId)
        {
            try
            {
                var landlordId = GetLandlordId();
                await _billingService.VerifyMeterReadingAsync(readingId, landlordId);
                return Ok(new { success = true, message = "Meter reading verified" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("meter-readings/{readingId}")]
        public async Task<IActionResult> DeleteMeterReading(int readingId)
        {
            try
            {
                var landlordId = GetLandlordId();
                await _billingService.DeleteMeterReadingAsync(readingId, landlordId);
                return Ok(new { success = true, message = "Meter reading deleted" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Invoices

        [HttpPost("invoices/generate")]
        public async Task<IActionResult> GenerateInvoice([FromBody] CreateInvoiceDto dto)
        {
            try
            {
                var landlordId = GetLandlordId();
                var result = await _billingService.GenerateInvoiceAsync(landlordId, dto.ContractId, dto.Month);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating invoice: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("invoices/generate-bulk")]
        public async Task<IActionResult> GenerateBulkInvoices([FromBody] BulkGenerateInvoicesDto dto)
        {
            try
            {
                var landlordId = GetLandlordId();
                var result = await _billingService.GenerateBulkInvoicesAsync(landlordId, dto);
                return Ok(new { success = true, data = result, count = result.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating bulk invoices: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("invoices/{invoiceId}")]
        public async Task<IActionResult> GetInvoice(int invoiceId)
        {
            try
            {
                var landlordId = GetLandlordId();
                var result = await _billingService.GetInvoiceAsync(invoiceId, landlordId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("invoices/month/{month}")]
        public async Task<IActionResult> GetInvoicesByMonth(string month)
        {
            try
            {
                var landlordId = GetLandlordId();
                var result = await _billingService.GetInvoicesByMonthAsync(landlordId, month);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("invoices/unpaid")]
        public async Task<IActionResult> GetUnpaidInvoices()
        {
            try
            {
                var landlordId = GetLandlordId();
                var result = await _billingService.GetUnpaidInvoicesAsync(landlordId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region PDF Export

        [HttpGet("invoices/{invoiceId}/export-pdf")]
        public async Task<IActionResult> ExportInvoicePdf(int invoiceId)
        {
            try
            {
                var landlordId = GetLandlordId();
                var pdfBytes = await _billingService.ExportInvoicePdfAsync(invoiceId, landlordId);
                return File(pdfBytes, "application/pdf", $"invoice-{invoiceId}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("invoices/month/{month}/export-pdf")]
        public async Task<IActionResult> ExportBulkInvoicesPdf(string month)
        {
            try
            {
                var landlordId = GetLandlordId();
                var pdfBytes = await _billingService.ExportBulkInvoicesPdfAsync(landlordId, month);
                return File(pdfBytes, "application/pdf", $"invoices-{month}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        private int GetLandlordId()
        {
            var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                ?? User.FindFirst("sub");
            
            if (int.TryParse(userIdClaim?.Value, out var userId))
                return userId;

            throw new UnauthorizedAccessException("Invalid user ID");
        }

        #endregion
    }
}
