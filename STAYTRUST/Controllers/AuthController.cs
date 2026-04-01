using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using STAYTRUST.Services;
using STAYTRUST.Data;
using STAYTRUST.Models;
using Microsoft.EntityFrameworkCore;

namespace STAYTRUST.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICaptchaService _captchaService;

        public AuthController(IAuthService authService, ICaptchaService captchaService)
        {
            _authService = authService;
            _captchaService = captchaService;
        }

        [HttpGet("captcha")]
        [Obsolete("Use Google reCAPTCHA v2 on the client side")]
        public IActionResult GetCaptcha()
        {
            return Ok(new { message = "Use Google reCAPTCHA v2" });
        }

        [HttpPost("login")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password, [FromForm] string? rememberMe, [FromForm(Name = "g-recaptcha-response")] string? recaptchaResponse)
        {
            bool isDev = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            if (!isDev)
            {
                if (string.IsNullOrEmpty(recaptchaResponse) || !await _captchaService.VerifyRecaptchaAsync(recaptchaResponse))
                {
                    return Redirect("/login?error=captcha");
                }
            }

            var token = await _authService.AuthenticateAsync(email, password);

            if (token == null)
            {
                // If the request is from a form, redirect back with an error
                return Redirect("/login?error=invalid");
            }

            // Set JWT in HttpOnly Cookie
            var isPersistent = rememberMe == "on";
            var expires = isPersistent ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddMinutes(1440);

            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Allow HTTP for local development
                SameSite = SameSiteMode.Lax,
                Expires = expires
            });

            // Get user role to determine redirect URL
            var userRole = await _authService.GetUserRoleAsync(email);
            if (userRole == "Landlord")
            {
                return Redirect("/landlord/home");
            }

            return Redirect("/");
        }

        [HttpPost("login-api")]
        public async Task<IActionResult> LoginApi([FromBody] LoginRequest request)
        {
            var token = await _authService.AuthenticateAsync(request.Email, request.Password);

            if (token == null)
                return Unauthorized(new { message = "Invalid email or password" });

            // Set JWT in HttpOnly Cookie
            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddMinutes(1440)
            });

            return Ok(new { message = "Login successful" });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("AuthToken");
            return Ok(new { message = "Logout successful" });
        }

        [HttpGet("login-google")]
        public IActionResult LoginGoogle()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback), "Auth")
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("signin-google")]
        public async Task<IActionResult> GoogleCallback()
        {
            // Authenticate with the Cookie scheme to get the Google identity
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return Redirect("/login?error=google_failed");

            var principalClaims = result.Principal?.Claims;
            var email = principalClaims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var fullName = principalClaims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? email ?? "Google User";

            if (string.IsNullOrEmpty(email))
                return Redirect("/login?error=google_no_email");

            // Find or create the user in the database
            var context = HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var authService = HttpContext.RequestServices.GetRequiredService<IAuthService>();

            var user = await context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                // Auto-register new Google users as Tenants with a random password
                await authService.RegisterUserAsync(fullName, email, Guid.NewGuid().ToString(), null, "Tenant");
                user = await context.Users
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.Email == email);
            }

            if (user == null)
                return Redirect("/login?error=google_register_failed");

            // Generate JWT directly (we can't use password-based auth for Google users)
            var token = authService.GenerateTokenForUser(user);

            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(30)
            });

            return Redirect(user.Role == "Landlord" ? "/landlord/home" : "/");
        }

        [HttpPost("refresh-token")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> RefreshToken([FromServices] AppDbContext context)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var user = await context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return Unauthorized();

            var newToken = _authService.GenerateTokenForUser(user);

            Response.Cookies.Append("AuthToken", newToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(30)
            });

            return Ok(new { message = "Token refreshed successfully" });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
