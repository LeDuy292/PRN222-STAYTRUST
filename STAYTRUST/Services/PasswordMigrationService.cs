using Microsoft.EntityFrameworkCore;
using STAYTRUST.Data;
using STAYTRUST.Models;

namespace STAYTRUST.Services
{
    public class PasswordMigrationService
    {
        public static async Task MigratePasswordsAsync(AppDbContext context)
        {
            var users = await context.Users.ToListAsync();
            var needsUpdate = false;

            foreach (var user in users)
            {
                // Check if password is already bcrypt hashed (bcrypt hashes start with $2a$, $2b$, or $2y$)
                if (!string.IsNullOrEmpty(user.Password) && 
                    !user.Password.StartsWith("$2a$") && 
                    !user.Password.StartsWith("$2b$") && 
                    !user.Password.StartsWith("$2y$"))
                {
                    // Hash the plain text password
                    user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                await context.SaveChangesAsync();
            }
        }
    }
}
