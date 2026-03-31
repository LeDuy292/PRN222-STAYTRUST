using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Amazon.S3;
using Amazon;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

using STAYTRUST.Components;
using STAYTRUST.Data;
using STAYTRUST.Services;
using STAYTRUST.Models;
using PayOS;
using Microsoft.EntityFrameworkCore;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
var builder = WebApplication.CreateBuilder(args);

// Add Database Context Factory
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Also add the scoped context for parts that still expect it during migration or for simple cases
builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

// Add Authentication Service
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRentalService, RentalService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IMessageService, MessageService>();

builder.Services.Configure<PayOSSettings>(builder.Configuration.GetSection("PayOSSettings"));
builder.Services.AddHttpClient<ICaptchaService, CaptchaService>();

// Add AWS S3 Service
var awsOptions = builder.Configuration.GetAWSOptions();
var accessKey = builder.Configuration["AWS:AccessKeyId"];
var secretKey = builder.Configuration["AWS:SecretKey"];
if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
{
    awsOptions.Credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
}
awsOptions.Region = Amazon.RegionEndpoint.GetBySystemName(builder.Configuration["AWS:Region"] ?? "ap-southeast-2");
builder.Services.AddAWSService<Amazon.S3.IAmazonS3>(awsOptions);
builder.Services.AddScoped<IAwsS3Service, AwsS3Service>();

// Add Image Validation Service
builder.Services.AddHttpClient<IImageValidationService, SightengineImageValidationService>();

// Add Listing Management Service
builder.Services.AddScoped<IListingManagementService, ListingManagementService>();

// Add Smart Billing Service
builder.Services.AddScoped<ISmartBillingService, SmartBillingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Add Gemini AI Service for Chatbot
builder.Services.AddHttpClient<IGeminiAIService, GeminiAIService>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };
    
    // We want to support reading the JWT from an HttpOnly Cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("AuthToken"))
            {
                context.Token = context.Request.Cookies["AuthToken"];
            }
            return Task.CompletedTask;
        }
    };
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "placeholder";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "placeholder";
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
});

// Add HttpContextAccessor and custom Authentication state provider for Blazor
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped(sp => 
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var client = new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };

    // Forward the JWT auth cookie as a Bearer token for API calls
    var token = httpContextAccessor.HttpContext?.Request.Cookies["AuthToken"];
    if (!string.IsNullOrEmpty(token))
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    return client;
});


builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, STAYTRUST.Providers.CustomAuthenticationStateProvider>();

builder.Services.AddAuthorization();

builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoomService, RoomService>();

var app = builder.Build();

// Migrate plain text passwords to bcrypt on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await PasswordMigrationService.MigratePasswordsAsync(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();

app.Run();
