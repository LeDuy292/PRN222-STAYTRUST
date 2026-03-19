using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using STAYTRUST.Data;
using STAYTRUST.Models;

namespace STAYTRUST.Services
{
    public class UserService : IUserService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public UserService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<User?> GetUserWithProfileAsync(int userId)
        {
            using var context = await _factory.CreateDbContextAsync();
            return await context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<List<User>> GetPotentialRoommatesAsync()
        {
            using var context = await _factory.CreateDbContextAsync();
            return await context.Users
                .Include(u => u.UserProfile)
                .Where(u => u.Role == "Tenant" && u.Status == true)
                .ToListAsync();
        }

        public async Task<bool> UpdateUserProfileAsync(int userId, string fullName, string phone, UserProfile profileUpdate)
        {
            using var context = await _factory.CreateDbContextAsync();
            var user = await context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return false;

            // Update User fields
            user.UserName = fullName;
            user.Phone = phone;

            // Update or Create UserProfile
            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile
                {
                    UserId = userId,
                    DateOfBirth = profileUpdate.DateOfBirth,
                    Gender = profileUpdate.Gender,
                    IdentityNumber = profileUpdate.IdentityNumber,
                    Address = profileUpdate.Address,
                    AvatarUrl = profileUpdate.AvatarUrl,
                    UpdatedAt = DateTime.Now
                };
                context.UserProfiles.Add(user.UserProfile);
            }
            else
            {
                user.UserProfile.DateOfBirth = profileUpdate.DateOfBirth;
                user.UserProfile.Gender = profileUpdate.Gender;
                user.UserProfile.IdentityNumber = profileUpdate.IdentityNumber;
                user.UserProfile.Address = profileUpdate.Address;
                user.UserProfile.AvatarUrl = profileUpdate.AvatarUrl;
                user.UserProfile.UpdatedAt = DateTime.Now;
            }

            try
            {
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user profile: {ex.Message}");
                return false;
            }
        }
    }
}
