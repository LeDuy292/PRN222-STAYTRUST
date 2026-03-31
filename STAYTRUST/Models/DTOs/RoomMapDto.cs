using System.Collections.Generic;

namespace STAYTRUST.Models.DTOs
{
    public class RoomMapDto
    {
        public int RoomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<string> Amenities { get; set; } = new();
    }
}
