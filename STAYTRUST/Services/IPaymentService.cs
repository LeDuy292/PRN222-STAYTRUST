using System.Threading.Tasks;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace STAYTRUST.Services
{
    public interface IPaymentService
    {
        Task<CreatePaymentLinkResponse> CreatePaymentLinkAsync(int invoiceId, decimal amount, string description, string returnUrl, string cancelUrl);
        Task<bool> VerifyWebhookDataAsync(Webhook webhookData);
    }
}
