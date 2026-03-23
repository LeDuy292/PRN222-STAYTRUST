using System;
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

    }

    public class AuthService : IAuthService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(IDbContextFactory<AppDbContext> factory, IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _factory = factory;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> RegisterUserAsync(string fullName, string email, string password, string? phoneNumber, string role)
        {
            using var context = await _factory.CreateDbContextAsync();
            if (await context.Users.AnyAsync(u => u.Email == email))
                return false;

            var user = new User
            {
                UserName = fullName,
                Email = email,
                Phone = phoneNumber ?? "", 
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                Status = true,
                CreatedAt = DateTime.Now
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<string?> AuthenticateAsync(string email, string password)
        {
            using var context = await _factory.CreateDbContextAsync();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                return null;
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("FullName", user.UserName ?? ""),
                new Claim("Role", user.Role ?? "Tenant"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["JwtSettings:ExpiryMinutes"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string?> AuthenticateGoogleAsync(string email, string name)
        {
            using var context = await _factory.CreateDbContextAsync();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    FullName = name ?? email,
                    UserName = name ?? email,
                    Email = email,
                    Phone = "0000000000", 
                    Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    Role = "Tenant",
                    Status = true,
                    CreatedAt = DateTime.Now
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Role, user.Role ?? "Tenant"),

                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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
    }
}
