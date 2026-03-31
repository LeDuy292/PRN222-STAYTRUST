namespace StaytrustAdmin.Models;

public class Room
{
    public int RoomId { get; set; }
    public int LandlordId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? Deposit { get; set; }
    public double? Area { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Image360Url { get; set; }
    
    public int Bedrooms { get; set; } = 1;
    public int Bathrooms { get; set; } = 1;
    public int? Floor { get; set; }
    public int? BuildingFloors { get; set; }
    public int? YearBuilt { get; set; }
    public string? Type { get; set; } = "Apartment";
    
    public bool Verified { get; set; }
    public bool Featured { get; set; }
    
    public double Rating { get; set; }
    public int Reviews { get; set; }
    public int Views { get; set; }
    public int Inquiries { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Dapper Virtual fields from JOIN Users
    public string? LandlordName { get; set; }
    
    // Dapper Virtual fields from JOIN RoomImages 
    public string? DefaultImageUrl { get; set; }
}
