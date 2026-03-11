using Microsoft.EntityFrameworkCore;
using STAYTRUST.Data;
using STAYTRUST.Models;

namespace STAYTRUST.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<bool> RegisterUserAsync(User user);
        Task<User?> LoginAsync(string username, string password);
    }

    public class UserService : IUserService
    {
        private readonly StayTrustDbContext _context;

        public UserService(StayTrustDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.Include(u => u.UserProfile).ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<bool> RegisterUserAsync(User user)
        {
            // Note: In a real app, hash the password!
            _context.Users.Add(user);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<User?> LoginAsync(string username, string password)
        {
            // Note: In a real app, compare hashed passwords!
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == username && u.Password == password);
        }
    }
}
