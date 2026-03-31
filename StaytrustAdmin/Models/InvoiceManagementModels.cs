namespace StaytrustAdmin.Models;

public class LandlordInvoiceSummary
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TotalRooms { get; set; }
    public bool IsActive { get; set; }
    public string Initials => string.Join("", FullName.Split(' ').Where(w => w.Length > 0).Take(2).Select(w => w[0]));
}

public class RoomInvoiceSummary
{
    public int RoomId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalInvoices { get; set; }
}

public class RoomInvoiceDetail
{
    public int InvoiceId { get; set; }
    public int ContractId { get; set; }
    public string Month { get; set; } = string.Empty;
    public decimal RoomPrice { get; set; }
    public decimal ElectricFee { get; set; }
    public decimal WaterFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty; // Unpaid, Paid
    public DateTime CreatedAt { get; set; }
    
    // Additional info from Tenant
    public string TenantName { get; set; } = string.Empty;
    public string TenantPhone { get; set; } = string.Empty;
}
