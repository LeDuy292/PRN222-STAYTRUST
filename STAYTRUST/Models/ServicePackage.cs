using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAYTRUST.Models
{
    public class ServicePackage
    {
        [Key]
        public int PackageId { get; set; }

        [StringLength(100)]
        public string? PackageName { get; set; }

        [Column(TypeName = "decimal(12, 2)")]
        public decimal? Price { get; set; }

        public int? DurationDays { get; set; }
    }
}
