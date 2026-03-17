using System;
using System.Collections.Generic;

namespace STAYTRUST.Models;

public partial class ServicePackage
{
    public int PackageId { get; set; }

    public string? PackageName { get; set; }

    public decimal? Price { get; set; }

    public int? DurationDays { get; set; }
}
