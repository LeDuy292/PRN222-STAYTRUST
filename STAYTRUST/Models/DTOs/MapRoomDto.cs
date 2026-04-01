namespace STAYTRUST.Models.DTOs
{
    public class MapRoomDto
    {
        public int RoomId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<string> Amenities { get; set; } = new List<string>();
        public string Address { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}
