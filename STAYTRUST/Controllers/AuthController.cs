using Microsoft.AspNetCore.Mvc;
using STAYTRUST.Services;

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
