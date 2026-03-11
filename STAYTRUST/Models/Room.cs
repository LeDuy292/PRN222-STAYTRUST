using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAYTRUST.Models
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        public int LandlordId { get; set; }

        [StringLength(200)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(12, 2)")]
        public decimal Price { get; set; }

        public double? Area { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Status { get; set; } // 'Available', 'Rented'

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("LandlordId")]
        public virtual User? Landlord { get; set; }

        public virtual ICollection<RoomImage> RoomImages { get; set; } = new List<RoomImage>();
        public virtual ICollection<RentalContract> RentalContracts { get; set; } = new List<RentalContract>();
        public virtual ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
    }
}
