namespace StaytrustAdmin.Models;

// ─── Section 1: KPI Overview ──────────────────────────────────────────────────
public class DashboardKpi
{
    public int TotalUsers { get; set; }
    public int TotalActiveRooms { get; set; }
    public int TotalActiveContracts { get; set; }
    public decimal RevenueThisMonth { get; set; }
    // Growth vs last month
    public int NewUsersThisMonth { get; set; }
    public int NewRoomsThisMonth { get; set; }
    public int NewContractsThisMonth { get; set; }
    public decimal RevenueLastMonth { get; set; }
    public double RevenueGrowthPct =>
        RevenueLastMonth == 0 ? 100 : Math.Round((double)((RevenueThisMonth - RevenueLastMonth) / RevenueLastMonth * 100), 1);
}

// ─── Section 2: Monthly Growth ────────────────────────────────────────────────
public class MonthlyPoint
{
    public string Month { get; set; } = "";  // "2024-01"
    public int    Count { get; set; }
    public decimal Amount { get; set; }
}

// ─── Section 3: Room Stats ────────────────────────────────────────────────────
public class RoomStatusCount
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
}

public class RoomTypeCount
{
    public string Type { get; set; } = "";
    public int Count { get; set; }
}

public class TopRoom
{
    public int    RoomId { get; set; }
    public string Title { get; set; } = "";
    public string LandlordName { get; set; } = "";
    public int    Views { get; set; }
    public int    FavoriteCount { get; set; }
    public double Rating { get; set; }
}

// ─── Section 4: Finance Stats ─────────────────────────────────────────────────
public class FinanceSummary
{
    public decimal TotalRevenuePaid { get; set; }
    public decimal TotalUnpaid { get; set; }
    public int     PaidInvoices { get; set; }
    public int     UnpaidInvoices { get; set; }
    public int     SuccessPayments { get; set; }
    public int     FailedPayments { get; set; }
}

public class TopLandlordRevenue
{
    public int    UserId { get; set; }
    public string FullName { get; set; } = "";
    public decimal TotalRevenue { get; set; }
    public int    RoomCount { get; set; }
}

// ─── Section 5: User Stats ────────────────────────────────────────────────────
public class UserRoleCount
{
    public string Role { get; set; } = "";
    public int Count { get; set; }
}

public class TopLandlordRooms
{
    public int    UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public int    RoomCount { get; set; }
}

// ─── Section 6: Activity Alerts ──────────────────────────────────────────────
public class ExpiringContract
{
    public int    ContractId { get; set; }
    public string TenantName { get; set; } = "";
    public string RoomTitle { get; set; } = "";
    public DateTime? EndDate { get; set; }
    public int DaysLeft { get; set; }
}

public class OverdueInvoice
{
    public int    InvoiceId { get; set; }
    public string TenantName { get; set; } = "";
    public string RoomTitle { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string Month { get; set; } = "";
}

public class PendingReport
{
    public int    ReportId { get; set; }
    public string ReportType { get; set; } = "";
    public string Description { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
