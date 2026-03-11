using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAYTRUST.Models
{
    public class MeterReading
    {
        [Key]
        public int ReadingId { get; set; }

        public int RoomId { get; set; }

        [StringLength(7)]
        public string? Month { get; set; } // YYYY-MM

        public int? ElectricOld { get; set; }
        public int? ElectricNew { get; set; }
        public int? WaterOld { get; set; }
        public int? WaterNew { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; }
    }
}
