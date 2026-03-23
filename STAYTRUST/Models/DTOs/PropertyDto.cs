namespace STAYTRUST.Models.DTOs
{
    public class PropertyListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Address { get; set; } = "";
        public long Price { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int Area { get; set; }
        public string Status { get; set; } = "";
        public double Rating { get; set; }
        public int Reviews { get; set; }
        public bool Featured { get; set; }
        public int Views { get; set; }
        public int Inquiries { get; set; }
        public List<string> Images { get; set; } = new();
        public string MainImage { get; set; } = "";
    }

    public class PropertyDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Address { get; set; } = "";
        public long Price { get; set; }
        public long Deposit { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int Area { get; set; }
        public int Floor { get; set; }
        public int BuildingFloors { get; set; }
        public int YearBuilt { get; set; }
        public string Status { get; set; } = "";
        public string Type { get; set; } = "";
        public double Rating { get; set; }
        public int Reviews { get; set; }
        public int Views { get; set; }
        public int Inquiries { get; set; }
        public bool Featured { get; set; }
        public bool Verified { get; set; }
        public string Description { get; set; } = "";
        public string Image360Url { get; set; } = "";
        public List<string> Images { get; set; } = new();
        public List<AmenityDto> Amenities { get; set; } = new();
        public TenantInfoDto? Tenant { get; set; }
        public FinancialInfoDto? Financials { get; set; }
        public List<MaintenanceRecordDto> Maintenance { get; set; } = new();
        public List<RevenueMonthDto> RevenueHistory { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class TenantInfoDto
    {
        public string Name { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public DateTime MoveInDate { get; set; }
        public DateTime LeaseEnd { get; set; }
        public string Avatar { get; set; } = "";
        public string Status { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
    }

    public class FinancialInfoDto
    {
        public long TotalRevenue { get; set; }
        public int OccupancyRate { get; set; }
        public long AvgMonthlyRevenue { get; set; }
    }

    public class MaintenanceRecordDto
    {
        public int Id { get; set; }
        public string Issue { get; set; } = "";
        public DateTime Date { get; set; }
        public long Cost { get; set; }
        public string Status { get; set; } = "";
    }

    public class RevenueMonthDto
    {
        public string Month { get; set; } = "";
        public long Amount { get; set; }
    }

    public class PropertyStatsDto
    {
        public int TotalProperties { get; set; }
        public int AvailableProperties { get; set; }
        public int OccupiedProperties { get; set; }
        public int TotalViews { get; set; }
        public double AverageRating { get; set; }
    }

    public class AmenityDto
    {
        public string Icon { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
