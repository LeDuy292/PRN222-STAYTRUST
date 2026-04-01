using System;
using Microsoft.Extensions.Caching.Distributed;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using STAYTRUST.Data;
using STAYTRUST.Models;

namespace STAYTRUST.Services
{
    public interface IAuthService
    {
        Task<bool> RegisterUserAsync(string fullName, string email, string password, string? phoneNumber, string role);
        Task<string?> AuthenticateAsync(string email, string password);
        Task<string?> GetUserRoleAsync(string email);
        Task<User?> GetCurrentUserAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Microsoft.Extensions.Caching.Distributed.IDistributedCache _cache;

        public AuthService(AppDbContext context, IConfiguration config, IHttpContextAccessor httpContextAccessor, Microsoft.Extensions.Caching.Distributed.IDistributedCache cache)
        {
            _context = context;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
        }

        public async Task<bool> RegisterUserAsync(string fullName, string email, string password, string? phoneNumber, string role)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
                return false;

            var user = new User
            {
                FullName = fullName,
                Email = email,
                UserName = email, // Use email as username for uniqueness
                Phone = phoneNumber ?? "", // Ensure Phone is not null as per schema
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                Status = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string?> AuthenticateAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return null;
            }

            bool isPasswordCorrect = false;
            
            if (string.IsNullOrEmpty(user.Password))
            {
                return null;
            }

            // A typical BCrypt hash starts with $2a$, $2b$, or $2y$ (60 chars)
            if (user.Password.StartsWith("$2") && user.Password.Length >= 60)
            {
                try {
                    isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, user.Password);
                } catch {
                    // Fallback to plain text if the hash is malformed despite $2 prefix
                    isPasswordCorrect = (password == user.Password);
                }
            }
            else
            {
                isPasswordCorrect = (password == user.Password);
            }

            if (!isPasswordCorrect)
            {
                return null;
            }

            // Single Session Enforcement: Generate and store session ID
            var sessionId = Guid.NewGuid().ToString();
            await _cache.SetStringAsync($"ActiveSession_{user.UserId}", sessionId, new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Convert.ToDouble(_config["JwtSettings:ExpiryMinutes"]))
            });

            // Generate JWT Token
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Role, user.Role ?? "Tenant"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("SessionId", sessionId)
            };

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["JwtSettings:ExpiryMinutes"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string?> GetUserRoleAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user?.Role;
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null || !httpContext.User.Identity?.IsAuthenticated == true)
            {
                return null;
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return null;
            }

            return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        }
    }
}
