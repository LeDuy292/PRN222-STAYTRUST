using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAYTRUST.Models
{
    public class Report
    {
        [Key]
        public int ReportId { get; set; }

        [StringLength(50)]
        public string? ReportType { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }
    }
}
