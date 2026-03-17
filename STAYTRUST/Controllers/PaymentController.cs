using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using STAYTRUST.Services;
using STAYTRUST.Data;
using Microsoft.EntityFrameworkCore;
using PayOS.Models.Webhooks;

namespace STAYTRUST.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly AppDbContext _context;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, AppDbContext context, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook([FromBody] Webhook webhookData)
        {
            try
            {
                var isValid = await _paymentService.VerifyWebhookDataAsync(webhookData);
                if (!isValid)
                {
                    return BadRequest("Invalid webhook signature");
                }

                // Process the verified data
                _logger.LogInformation("PayOS Webhook received and verified: {OrderCode}, Status: {Code}", 
                    webhookData.Data.OrderCode, webhookData.Code);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PayOS webhook");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
