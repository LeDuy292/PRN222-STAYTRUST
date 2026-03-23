namespace STAYTRUST.Models.DTOs
{
    public class MeterReadingDto
    {
        public int ReadingId { get; set; }
        public int RoomId { get; set; }
        public string RoomTitle { get; set; }
        public string Month { get; set; }
        
        // Electric
        public int ElectricOld { get; set; }
        public int ElectricNew { get; set; }
        public int ElectricUsage => ElectricNew - ElectricOld;
        
        // Water
        public int WaterOld { get; set; }
        public int WaterNew { get; set; }
        public int WaterUsage => WaterNew - WaterOld;
        
        // Status
        public string Status { get; set; } = "Submitted";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateMeterReadingDto
    {
        public int RoomId { get; set; }
        public string Month { get; set; }
        public int ElectricOld { get; set; }
        public int ElectricNew { get; set; }
        public int WaterOld { get; set; }
        public int WaterNew { get; set; }
    }

    public class BulkMeterReadingDto
    {
        public string Month { get; set; }
        public List<CreateMeterReadingDto> Readings { get; set; }
    }
}
