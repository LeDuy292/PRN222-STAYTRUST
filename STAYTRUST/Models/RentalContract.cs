using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAYTRUST.Models
{
    public class RentalContract
    {
        [Key]
        public int ContractId { get; set; }

        public int RoomId { get; set; }

        public int TenantId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Column(TypeName = "decimal(12, 2)")]
        public decimal? Deposit { get; set; }

        [StringLength(20)]
        public string? Status { get; set; } // 'Active', 'Expired', 'Cancelled'

        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; }

        [ForeignKey("TenantId")]
        public virtual User? Tenant { get; set; }

        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
