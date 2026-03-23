using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAYTRUST.Models.DTOs;
using STAYTRUST.Services;
using System.Security.Claims;

namespace STAYTRUST.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ListingsController : ControllerBase
    {
        private readonly IListingManagementService _listingService;
        private readonly ILogger<ListingsController> _logger;

        public ListingsController(IListingManagementService listingService, ILogger<ListingsController> logger)
        {
            _listingService = listingService;
            _logger = logger;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim?.Value, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get all listings for the current landlord
        /// </summary>
        [HttpGet("my-listings")]
        public async Task<IActionResult> GetMyListings()
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User not authenticated");

            try
            {
                var listings = await _listingService.GetLandlordListingsAsync(userId);
                return Ok(new { success = true, data = listings });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting listings: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific listing by ID
        /// </summary>
        [HttpGet("{roomId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetListing(int roomId)
        {
            try
            {
                var listing = await _listingService.GetListingAsync(roomId);
                if (listing == null)
                    return NotFound(new { success = false, message = "Listing not found" });

                return Ok(new { success = true, data = listing });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Create a new listing (Draft status)
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateListing([FromBody] CreateRoomListingDto dto)
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User not authenticated");

            try
            {
                var listing = await _listingService.CreateListingAsync(userId, dto);
                return Ok(new { success = true, data = listing, message = "Listing created successfully in Draft status" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating listing: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Update listing details
        /// </summary>
        [HttpPut("{roomId}/update")]
        public async Task<IActionResult> UpdateListing(int roomId, [FromBody] UpdateRoomListingDto dto)
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User not authenticated");

            dto.RoomId = roomId;

            try
            {
                var success = await _listingService.UpdateListingAsync(userId, dto);
                return Ok(new { success = true, message = "Listing updated successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Update listing status (Draft -> Pending -> Active, etc.)
        /// </summary>
        [HttpPut("{roomId}/status")]
        public async Task<IActionResult> UpdateStatus(int roomId, [FromBody] ListingStatusUpdateDto dto)
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User not authenticated");

            try
            {
                var success = await _listingService.UpdateListingStatusAsync(userId, roomId, dto.NewStatus);
                return Ok(new { success = true, message = $"Listing status updated to {dto.NewStatus}" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Upload image to listing (max 10 images)
        /// </summary>
        [HttpPost("{roomId}/upload-image")]
        public async Task<IActionResult> UploadImage(int roomId, [FromForm] RoomImageUploadDto dto)
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User not authenticated");

            if (dto.Image == null || dto.Image.Length == 0)
                return BadRequest(new { success = false, message = "No image provided" });

            try
            {
                var imageUrl = await _listingService.UploadListingImageAsync(roomId, userId, dto.Image, dto.Is360);
                return Ok(new 
                { 
                    success = true, 
                    data = new { imageUrl, is360 = dto.Is360 },
                    message = "Image uploaded successfully" 
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading image: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Remove image from listing
        /// </summary>
        [HttpDelete("image/{imageId}")]
        public async Task<IActionResult> RemoveImage(int imageId)
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User not authenticated");

            try
            {
                var success = await _listingService.RemoveImageAsync(imageId, userId);
                return Ok(new { success = true, message = "Image removed successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Submit listing for review (change from Draft to Pending)
        /// </summary>
        [HttpPost("{roomId}/submit-for-review")]
        public async Task<IActionResult> SubmitForReview(int roomId)
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User not authenticated");

            try
            {
                // Validate images before submitting
                await _listingService.ValidateAndApproveImagesAsync(roomId);

                var success = await _listingService.UpdateListingStatusAsync(userId, roomId, "Pending");
                return Ok(new { success = true, message = "Listing submitted for review" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Delete listing (only Draft listings)
        /// </summary>
        [HttpDelete("{roomId}")]
        public async Task<IActionResult> DeleteListing(int roomId)
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized("User not authenticated");

            try
            {
                var success = await _listingService.DeleteListingAsync(userId, roomId);
                return Ok(new { success = true, message = "Listing deleted successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
