using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAYTRUST.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        public int ContractId { get; set; }

        [StringLength(7)]
        public string? Month { get; set; }

        [Column(TypeName = "decimal(12, 2)")]
        public decimal? RoomPrice { get; set; }

        [Column(TypeName = "decimal(12, 2)")]
        public decimal? ElectricFee { get; set; }

        [Column(TypeName = "decimal(12, 2)")]
        public decimal? WaterFee { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? TotalAmount { get; private set; }

        [StringLength(20)]
        public string? Status { get; set; } // 'Unpaid', 'Paid'

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ContractId")]
        public virtual RentalContract? RentalContract { get; set; }

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
