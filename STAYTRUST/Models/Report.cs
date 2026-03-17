using System;
using System.Collections.Generic;

namespace STAYTRUST.Models;

public partial class Report
{
    public int ReportId { get; set; }

    public string? ReportType { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }
}
