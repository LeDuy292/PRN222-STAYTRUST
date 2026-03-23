namespace STAYTRUST.Models.DTOs
{
    public class InvoiceDto
    {
        public int InvoiceId { get; set; }
        public int ContractId { get; set; }
        public int RoomId { get; set; }
        public string RoomTitle { get; set; }
        public string TenantName { get; set; }
        public string TenantEmail { get; set; }
        public string Month { get; set; }
        
        // Charges
        public decimal RoomPrice { get; set; }
        public decimal ElectricFee { get; set; }
        public decimal WaterFee { get; set; }
        public decimal TotalAmount { get; set; }
        
        // Usage Details
        public int ElectricUsage { get; set; }
        public int WaterUsage { get; set; }
        public decimal ElectricPrice { get; set; }
        public decimal WaterPrice { get; set; }
        
        // Status
        public string Status { get; set; } = "Unpaid";
        public DateTime CreatedAt { get; set; }
    }

    public class CreateInvoiceDto
    {
        public int ContractId { get; set; }
        public string Month { get; set; }
        public decimal RoomPrice { get; set; }
        public decimal ElectricFee { get; set; }
        public decimal WaterFee { get; set; }
    }

    public class BulkGenerateInvoicesDto
    {
        public string Month { get; set; }
        public List<int> RoomIds { get; set; }
    }

    public class InvoiceDetailDto
    {
        public InvoiceDto Invoice { get; set; }
        public MeterReadingDto MeterReading { get; set; }
    }
}
