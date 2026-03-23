using System;
using System.Collections.Generic;

namespace STAYTRUST.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int ContractId { get; set; }

    public string? Month { get; set; }

    public decimal RoomPrice { get; set; }

    public decimal ElectricFee { get; set; }

    public decimal WaterFee { get; set; }

    public decimal? TotalAmount { get; set; }

    public string Status { get; set; } = "Unpaid";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual RentalContract Contract { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
