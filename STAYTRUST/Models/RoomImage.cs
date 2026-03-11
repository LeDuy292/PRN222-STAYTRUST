using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAYTRUST.Models
{
    public class RoomImage
    {
        [Key]
        public int ImageId { get; set; }

        public int RoomId { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public bool Approved { get; set; } = false;

        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; }
    }
}
