using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STAYTRUST.Data;
using STAYTRUST.Models;
using STAYTRUST.Models.DTOs;

namespace STAYTRUST.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PropertiesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PropertiesController> _logger;

        public PropertiesController(AppDbContext context, ILogger<PropertiesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all properties for current landlord
        /// </summary>
        [HttpGet("my-properties")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMyProperties()
        {
            try
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation($"GetMyProperties called - User ID: {userIdStr}");
                
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int landlordId))
                {
                    _logger.LogWarning("Invalid or missing user ID - returning all properties for testing");
                    var allProperties = await _context.Rooms
                        .Include(r => r.RoomImages)
                        .Include(r => r.Landlord)
                        .OrderByDescending(r => r.CreatedAt)
                        .Select(r => new PropertyListDto
                        {
                            Id = r.RoomId,
                            Title = r.Title ?? "",
                            Address = r.Address ?? "",
                            Price = (long)r.Price,
                            Bedrooms = r.Bedrooms,
                            Bathrooms = r.Bathrooms,
                            Area = (int)(r.Area ?? 0),
                            Status = r.Status ?? "Active",
                            Rating = r.Rating,
                            Reviews = r.Reviews,
                            Featured = r.Featured,
                            Views = r.Views,
                            Inquiries = r.Inquiries,
                            Images = r.RoomImages.Select(i => i.ImageUrl ?? "").ToList()
                        })
                        .ToListAsync();

                    _logger.LogInformation($"Returned {allProperties.Count} properties");
                    return Ok(new { data = allProperties, count = allProperties.Count });
                }
                
                _logger.LogInformation($"Fetching properties for landlord {landlordId}");
                
                var properties = await _context.Rooms
                    .Where(r => r.LandlordId == landlordId)
                    .Include(r => r.RoomImages)
                    .Include(r => r.Landlord)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new PropertyListDto
                    {
                        Id = r.RoomId,
                        Title = r.Title ?? "",
                        Address = r.Address ?? "",
                        Price = (long)r.Price,
                        Bedrooms = r.Bedrooms,
                        Bathrooms = r.Bathrooms,
                        Area = (int)(r.Area ?? 0),
                        Status = r.Status ?? "Active",
                        Rating = r.Rating,
                        Reviews = r.Reviews,
                        Featured = r.Featured,
                        Views = r.Views,
                        Inquiries = r.Inquiries,
                        Images = r.RoomImages.Select(i => i.ImageUrl ?? "").ToList()
                    })
                    .ToListAsync();

                _logger.LogInformation($"Found {properties.Count} properties for landlord {landlordId}");
                return Ok(new { data = properties, count = properties.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get property by ID (public endpoint)
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPropertyById(int id)
        {
            try
            {
                var property = await _context.Rooms
                    .Include(r => r.RoomImages)
                    .FirstOrDefaultAsync(r => r.RoomId == id);

                if (property == null)
                    return NotFound(new { message = "Property not found" });

                var amenities = new List<AmenityDto>
                { 
                    new() { Icon = "wifi", Name = "High-Speed WiFi" },
                    new() { Icon = "car", Name = "Parking Space" },
                    new() { Icon = "swimming-pool", Name = "Swimming Pool" },
                    new() { Icon = "dumbbell", Name = "Fitness Center" },
                    new() { Icon = "shield-alt", Name = "24/7 Security" },
                    new() { Icon = "camera", Name = "CCTV Monitoring" },
                };

                // Get tenant information if property is occupied
                TenantInfoDto? tenantInfo = null;
                var activeContract = await _context.RentalContracts
                    .Include(c => c.Tenant)
                    .FirstOrDefaultAsync(c => c.RoomId == id && c.Status == "Active");

                if (activeContract != null)
                {
                    tenantInfo = new TenantInfoDto
                    {
                        Name = activeContract.Tenant?.UserName ?? "Unknown",
                        FullName = activeContract.Tenant?.FullName ?? "Unknown",
                        Email = activeContract.Tenant?.Email ?? "",
                        Phone = activeContract.Tenant?.Phone ?? "",
                        MoveInDate = activeContract.StartDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
                        LeaseEnd = activeContract.EndDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
                        Avatar = "T", // First letter of tenant name for avatar
                        Status = "active",
                        PaymentStatus = "on-time"
                    };
                }

                var response = new PropertyDetailDto
                {
                    Id = property.RoomId,
                    Title = property.Title ?? "",
                    Address = property.Address ?? "",
                    Price = (long)property.Price,
                    Deposit = (long)property.Deposit,
                    Bedrooms = property.Bedrooms,
                    Bathrooms = property.Bathrooms,
                    Area = (int)(property.Area ?? 0),
                    Floor = property.Floor,
                    BuildingFloors = property.BuildingFloors,
                    YearBuilt = property.YearBuilt,
                    Status = property.Status ?? "Active",
                    Type = property.Type ?? "Apartment",
                    Rating = property.Rating,
                    Reviews = property.Reviews,
                    Views = property.Views,
                    Inquiries = property.Inquiries,
                    Featured = property.Featured,
                    Verified = property.Verified,
                    Description = property.Description ?? "",
                    Image360Url = property.Image360Url ?? "",
                    Images = property.RoomImages.Select(i => i.ImageUrl ?? "").ToList(),
                    Amenities = amenities,
                    Tenant = tenantInfo,
                    CreatedAt = property.CreatedAt ?? DateTime.Now
                };

                return Ok(new { data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching property {id}: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get property stats for landlord dashboard
        /// </summary>
        [HttpGet("stats/my-stats")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMyStats()
        {
            try
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                List<Room> properties;
                
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int landlordId))
                {
                    properties = await _context.Rooms.ToListAsync();
                }
                else
                {
                    properties = await _context.Rooms
                        .Where(r => r.LandlordId == landlordId)
                        .ToListAsync();
                }

                var stats = new PropertyStatsDto
                {
                    TotalProperties = properties.Count,
                    AvailableProperties = properties.Count(p => p.Status == "Available" || p.Status == "Active"),
                    OccupiedProperties = properties.Count(p => p.Status == "Rented" || p.Status == "Occupied"),
                    TotalViews = properties.Sum(p => p.Views),
                    AverageRating = properties.Any() ? properties.Average(p => p.Rating) : 0
                };

                return Ok(new { data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Search properties by address or title
        /// </summary>
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchProperties([FromQuery] string? search, [FromQuery] string? status)
        {
            try
            {
                var query = _context.Rooms
                    .Include(r => r.RoomImages)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(r => 
                        r.Title!.Contains(search) || 
                        r.Address!.Contains(search));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(r => r.Status == status);
                }

                var properties = await query
                    .OrderByDescending(r => r.Featured)
                    .ThenByDescending(r => r.Rating)
                    .Select(r => new PropertyListDto
                    {
                        Id = r.RoomId,
                        Title = r.Title ?? "",
                        Address = r.Address ?? "",
                        Price = (long)r.Price,
                        Bedrooms = r.Bedrooms,
                        Bathrooms = r.Bathrooms,
                        Area = (int)(r.Area ?? 0),
                        Status = r.Status ?? "Active",
                        Rating = r.Rating,
                        Reviews = r.Reviews,
                        Featured = r.Featured,
                        Views = r.Views,
                        Inquiries = r.Inquiries,
                        Images = r.RoomImages.Select(i => i.ImageUrl ?? "").ToList()
                    })
                    .ToListAsync();

                return Ok(new { data = properties, count = properties.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching properties: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
