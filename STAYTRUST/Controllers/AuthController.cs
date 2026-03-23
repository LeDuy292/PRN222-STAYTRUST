using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using STAYTRUST.Services;
using System.Security.Claims;

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

        [HttpGet("login-google")]
        public IActionResult LoginGoogle(string returnUrl = "/")
        {
            var properties = new AuthenticationProperties { RedirectUri = $"/api/auth/google-callback?returnUrl={returnUrl}" };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback(string returnUrl = "/")
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return Redirect("/login?error=google_failed");

            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
                return Redirect("/login?error=no_email");

            var token = await _authService.AuthenticateGoogleAsync(email, name ?? email);
            
            if (token == null)
            {
                return Redirect("/login?error=invalid");
            }

            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddMinutes(1440)
            });

            return Redirect(returnUrl ?? "/");
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
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
