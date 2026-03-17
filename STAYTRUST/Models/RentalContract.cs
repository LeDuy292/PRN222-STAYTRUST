using System;
using System.Collections.Generic;

namespace STAYTRUST.Models;

public partial class RentalContract
{
    public int ContractId { get; set; }

    public int RoomId { get; set; }

    public int TenantId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal? Deposit { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual Room Room { get; set; } = null!;

    public virtual User Tenant { get; set; } = null!;
}
