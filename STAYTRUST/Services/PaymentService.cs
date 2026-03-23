using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using STAYTRUST.Models;
using STAYTRUST.Data;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace STAYTRUST.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly PayOSClient _payOSClient;
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IOptions<PayOSSettings> settings, IDbContextFactory<AppDbContext> factory, ILogger<PaymentService> logger)
        {
            var options = new PayOSOptions
            {
                ClientId = settings.Value.ClientId,
                ApiKey = settings.Value.ApiKey,
                ChecksumKey = settings.Value.ChecksumKey
            };
            _payOSClient = new PayOSClient(options);
            _factory = factory;
            _logger = logger;
        }

        public async Task<CreatePaymentLinkResponse> CreatePaymentLinkAsync(int invoiceId, decimal amount, string description, string returnUrl, string cancelUrl)
        {
            try
            {
                // PayOS requires a long order code
                long orderCode = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                
                var paymentData = new CreatePaymentLinkRequest
                {
                    OrderCode = orderCode,
                    Amount = (long)amount,
                    Description = description.Length > 25 ? description.Substring(0, 22) + "..." : description,
                    CancelUrl = cancelUrl,
                    ReturnUrl = returnUrl,
                    Items = new List<PaymentLinkItem> { new PaymentLinkItem { Name = description, Quantity = 1, Price = (long)amount } }
                };

                var result = await _payOSClient.PaymentRequests.CreateAsync(paymentData);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayOS payment link for invoice {InvoiceId}", invoiceId);
                throw;
            }
        }

        public async Task<bool> VerifyWebhookDataAsync(Webhook webhookData)
        {
            try
            {
                var verifiedData = await _payOSClient.Webhooks.VerifyAsync(webhookData);
                return verifiedData != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying PayOS webhook data");
                return false;
            }
        }
    }
}
