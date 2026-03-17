using System.Threading.Tasks;

namespace STAYTRUST.Services
{
    public interface ICaptchaService
    {
        Task<bool> VerifyRecaptchaAsync(string token);
    }
}
