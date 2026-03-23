using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using STAYTRUST.Data;
using STAYTRUST.Models;
using STAYTRUST.Models.DTOs;

namespace STAYTRUST.Services
{
    public interface IListingManagementService
    {
        Task<RoomListingResponseDto> CreateListingAsync(int landlordId, CreateRoomListingDto dto);
        Task<RoomListingResponseDto?> GetListingAsync(int roomId);
        Task<List<RoomListingResponseDto>> GetLandlordListingsAsync(int landlordId);
        Task<bool> UpdateListingAsync(int landlordId, UpdateRoomListingDto dto);
        Task<bool> DeleteListingAsync(int landlordId, int roomId);
        Task<bool> UpdateListingStatusAsync(int landlordId, int roomId, string newStatus);
        Task<string> UploadListingImageAsync(int roomId, int landlordId, IFormFile image, bool is360);
        Task<bool> RemoveImageAsync(int imageId, int landlordId);
        Task ValidateAndApproveImagesAsync(int roomId);
        Task AutoHideExpiredListingsAsync();
    }

    public class ListingManagementService : IListingManagementService
    {
        private readonly AppDbContext _context;
        private readonly IAwsS3Service _s3Service;
        private readonly IImageValidationService _imageValidationService;
        private readonly ILogger<ListingManagementService> _logger;

        public ListingManagementService(
            AppDbContext context,
            IAwsS3Service s3Service,
            IImageValidationService imageValidationService,
            ILogger<ListingManagementService> logger)
        {
            _context = context;
            _s3Service = s3Service;
            _imageValidationService = imageValidationService;
            _logger = logger;
        }

        public async Task<RoomListingResponseDto> CreateListingAsync(int landlordId, CreateRoomListingDto dto)
        {
            try
            {
                var room = new Room
                {
                    LandlordId = landlordId,
                    Title = dto.Title,
                    Description = dto.Description,
                    Address = dto.Address,
                    Price = dto.Price,
                    Deposit = dto.Deposit,
                    Area = dto.Area,
                    Bedrooms = dto.Bedrooms,
                    Bathrooms = dto.Bathrooms,
                    Floor = dto.Floor,
                    BuildingFloors = dto.BuildingFloors,
                    YearBuilt = dto.YearBuilt,
                    Type = dto.Type,
                    Image360Url = dto.Image360Url,
                    Status = "Draft",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Rooms.Add(room);
                await _context.SaveChangesAsync();

                return MapToResponseDto(room);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating listing: {ex.Message}");
                throw;
            }
        }

        public async Task<RoomListingResponseDto?> GetListingAsync(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomImages)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);

            return room == null ? null : MapToResponseDto(room);
        }

        public async Task<List<RoomListingResponseDto>> GetLandlordListingsAsync(int landlordId)
        {
            var rooms = await _context.Rooms
                .Where(r => r.LandlordId == landlordId)
                .Include(r => r.RoomImages)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return rooms.Select(MapToResponseDto).ToList();
        }

        public async Task<bool> UpdateListingAsync(int landlordId, UpdateRoomListingDto dto)
        {
            var room = await _context.Rooms.FindAsync(dto.RoomId);
            if (room == null || room.LandlordId != landlordId)
                throw new UnauthorizedAccessException("You don't have permission to update this listing.");

            if (!string.IsNullOrEmpty(dto.Title))
                room.Title = dto.Title;
            if (!string.IsNullOrEmpty(dto.Description))
                room.Description = dto.Description;
            if (!string.IsNullOrEmpty(dto.Address))
                room.Address = dto.Address;
            if (dto.Price.HasValue && dto.Price > 0)
                room.Price = dto.Price.Value;
            if (dto.Deposit.HasValue && dto.Deposit >= 0)
                room.Deposit = dto.Deposit.Value;
            if (dto.Area.HasValue && dto.Area > 0)
                room.Area = (double)dto.Area.Value;
            if (dto.Bedrooms.HasValue)
                room.Bedrooms = dto.Bedrooms.Value;
            if (dto.Bathrooms.HasValue)
                room.Bathrooms = dto.Bathrooms.Value;
            if (dto.Floor.HasValue)
                room.Floor = dto.Floor.Value;
            if (dto.BuildingFloors.HasValue)
                room.BuildingFloors = dto.BuildingFloors.Value;
            if (dto.YearBuilt.HasValue)
                room.YearBuilt = dto.YearBuilt.Value;
            if (!string.IsNullOrEmpty(dto.Type))
                room.Type = dto.Type;

            room.UpdatedAt = DateTime.Now;

            _context.Rooms.Update(room);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteListingAsync(int landlordId, int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomImages)
                .FirstOrDefaultAsync(r => r.RoomId == roomId && r.LandlordId == landlordId);

            if (room == null)
                throw new UnauthorizedAccessException("You don't have permission to delete this listing.");

            try
            {
                // Delete images from S3
                if (room.RoomImages != null && room.RoomImages.Count > 0)
                {
                    foreach (var image in room.RoomImages)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(image.ImageUrl))
                            {
                                // Extract S3 key from URL (format: https://bucket.s3.region.amazonaws.com/key)
                                var fileKey = ExtractS3KeyFromUrl(image.ImageUrl);
                                await _s3Service.DeleteImageAsync(fileKey);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Failed to delete image {image.ImageUrl} from S3: {ex.Message}");
                            // Continue deleting other images and the listing
                        }
                    }
                }

                // Delete 360 image if exists
                if (!string.IsNullOrEmpty(room.Image360Url))
                {
                    try
                    {
                        var fileKey = ExtractS3KeyFromUrl(room.Image360Url);
                        await _s3Service.DeleteImageAsync(fileKey);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to delete 360 image {room.Image360Url} from S3: {ex.Message}");
                    }
                }

                // Delete all room images from DB (cascade delete should handle this)
                _context.RoomImages.RemoveRange(room.RoomImages);

                // Delete the room
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Listing {roomId} deleted successfully by landlord {landlordId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting listing {roomId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateListingStatusAsync(int landlordId, int roomId, string newStatus)
        {
            var validStatuses = new[] { "Draft", "Pending", "Active", "Hidden", "Expired" };
            if (!validStatuses.Contains(newStatus))
                throw new ArgumentException("Invalid status");

            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null || room.LandlordId != landlordId)
                throw new UnauthorizedAccessException("You don't have permission to update this listing.");

            room.Status = newStatus;
            if (newStatus == "Active")
            {
                // Set expiration to 15 days from now (or based on service package)
                // This would need to be updated based on service package logic
            }

            _context.Rooms.Update(room);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> UploadListingImageAsync(int roomId, int landlordId, IFormFile image, bool is360)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null || room.LandlordId != landlordId)
                throw new UnauthorizedAccessException("You don't have permission to upload images to this listing.");

            // Check image count (max 10)
            var existingImages = await _context.RoomImages.CountAsync(i => i.RoomId == roomId);
            if (existingImages >= 10)
                throw new InvalidOperationException("Maximum 10 images allowed per listing.");

            // Upload to S3
            using (var stream = image.OpenReadStream())
            {
                var imageUrl = await _s3Service.UploadImageAsync(stream, image.FileName);

                // Validate image with Sightengine
                var validationResult = await _imageValidationService.ValidateImageAsync(imageUrl);

                var roomImage = new RoomImage
                {
                    RoomId = roomId,
                    ImageUrl = imageUrl,
                    Approved = validationResult.IsValid // Auto-approve if valid
                };

                if (is360)
                {
                    room.Image360Url = imageUrl;
                }

                _context.RoomImages.Add(roomImage);
                if (is360)
                    _context.Rooms.Update(room);

                await _context.SaveChangesAsync();

                return imageUrl;
            }
        }

        public async Task<bool> RemoveImageAsync(int imageId, int landlordId)
        {
            var image = await _context.RoomImages
                .Include(i => i.Room)
                .FirstOrDefaultAsync(i => i.ImageId == imageId && i.Room!.LandlordId == landlordId);

            if (image == null)
                throw new UnauthorizedAccessException("You don't have permission to remove this image.");

            _context.RoomImages.Remove(image);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ValidateAndApproveImagesAsync(int roomId)
        {
            var images = await _context.RoomImages
                .Where(i => i.RoomId == roomId && i.Approved != true)
                .ToListAsync();

            foreach (var image in images)
            {
                var validationResult = await _imageValidationService.ValidateImageAsync(image.ImageUrl);
                image.Approved = validationResult.IsValid;
            }

            await _context.SaveChangesAsync();
        }

        public async Task AutoHideExpiredListingsAsync()
        {
            // Get listings that are Active and older than 15 days without a service package
            var fifteenDaysAgo = DateTime.Now.AddDays(-15);
            var expiredListings = await _context.Rooms
                .Where(r => r.Status == "Active" && r.CreatedAt < fifteenDaysAgo)
                .ToListAsync();

            foreach (var listing in expiredListings)
            {
                listing.Status = "Hidden";
            }

            if (expiredListings.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        private RoomListingResponseDto MapToResponseDto(Room room)
        {
            return new RoomListingResponseDto
            {
                RoomId = room.RoomId,
                LandlordId = room.LandlordId,
                Title = room.Title ?? string.Empty,
                Description = room.Description ?? string.Empty,
                Address = room.Address ?? string.Empty,
                Price = (long?)room.Price,
                Deposit = (long?)room.Deposit,
                Area = (float)(room.Area ?? 0),
                Bedrooms = room.Bedrooms,
                Bathrooms = room.Bathrooms,
                Floor = room.Floor,
                BuildingFloors = room.BuildingFloors,
                YearBuilt = room.YearBuilt,
                Type = room.Type ?? "Apartment",
                Status = room.Status ?? "Draft",
                Image360Url = room.Image360Url,
                CreatedAt = room.CreatedAt ?? DateTime.Now,
                Images = room.RoomImages?.Select(i => new RoomImageDto
                {
                    ImageId = i.ImageId,
                    ImageUrl = i.ImageUrl ?? string.Empty,
                    IsApproved = i.Approved ?? false,
                    Is360 = i.ImageUrl == room.Image360Url
                }).ToList() ?? new()
            };
        }

        /// <summary>
        /// Extract S3 object key from full S3 URL
        /// Example: https://bucket.s3.ap-southeast-2.amazonaws.com/rooms/2024/01/01/guid_filename.jpg
        /// Returns: rooms/2024/01/01/guid_filename.jpg
        /// </summary>
        private string ExtractS3KeyFromUrl(string s3Url)
        {
            try
            {
                // Parse the URL to get the path component
                var uri = new Uri(s3Url);
                var path = uri.AbsolutePath;
                
                // Remove leading slash if present
                if (path.StartsWith('/'))
                    path = path.Substring(1);
                
                return path;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error extracting S3 key from URL {s3Url}: {ex.Message}");
                // Return the URL as-is if parsing fails, let S3 service handle the error
                return s3Url;
            }
        }
    }
}
