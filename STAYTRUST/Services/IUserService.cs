using System.Threading.Tasks;
using System.Collections.Generic;
using STAYTRUST.Models;

namespace STAYTRUST.Services
{
    public interface IUserService
    {
        Task<User?> GetUserWithProfileAsync(int userId);
        Task<bool> UpdateUserProfileAsync(int userId, string fullName, string phone, UserProfile profileUpdate);
        Task<List<User>> GetPotentialRoommatesAsync();
    }
}
