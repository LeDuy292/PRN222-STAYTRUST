using System;
using System.Collections.Generic;

namespace STAYTRUST.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int InvoiceId { get; set; }

    public string? PaymentMethod { get; set; }

    public DateTime? PaymentDate { get; set; }

    public decimal? Amount { get; set; }

    public string? Status { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
}
