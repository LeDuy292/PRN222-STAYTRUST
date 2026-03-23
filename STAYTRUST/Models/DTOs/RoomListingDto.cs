using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace STAYTRUST.Models.DTOs
{
    public class CreateRoomListingDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public long Price { get; set; }
        public long Deposit { get; set; }
        public double Area { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int Floor { get; set; }
        public int BuildingFloors { get; set; }
        public int YearBuilt { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Image360Url { get; set; } = string.Empty;
        public List<string> Amenities { get; set; } = new List<string>();
    }

    public class UpdateRoomListingDto
    {
        public int RoomId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public long? Price { get; set; }
        public long? Deposit { get; set; }
        public double? Area { get; set; }
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public int? Floor { get; set; }
        public int? BuildingFloors { get; set; }
        public int? YearBuilt { get; set; }
        public string? Type { get; set; }
        public List<string> Amenities { get; set; } = new List<string>();
    }

    public class RoomListingResponseDto
    {
        public int RoomId { get; set; }
        public int LandlordId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public long? Price { get; set; }
        public long? Deposit { get; set; }
        public float Area { get; set; }
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public int? Floor { get; set; }
        public int? BuildingFloors { get; set; }
        public int? YearBuilt { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Image360Url { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<RoomImageDto> Images { get; set; } = new List<RoomImageDto>();
    }

    public class RoomImageDto
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public bool Is360 { get; set; }
    }

    public class RoomImageUploadDto
    {
        public IFormFile Image { get; set; } = null!;
        public bool Is360 { get; set; }
    }

    public class ListingStatusUpdateDto
    {
        public int RoomId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }
}
