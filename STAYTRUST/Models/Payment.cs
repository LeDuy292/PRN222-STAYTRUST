using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAYTRUST.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int InvoiceId { get; set; }

        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        public DateTime? PaymentDate { get; set; }

        [Column(TypeName = "decimal(12, 2)")]
        public decimal? Amount { get; set; }

        [StringLength(20)]
        public string? Status { get; set; } // 'Success', 'Failed'

        [ForeignKey("InvoiceId")]
        public virtual Invoice? Invoice { get; set; }
    }
}
