using System;
using System.Collections.Generic;

namespace STAYTRUST.Models;

public partial class MeterReading
{
    public int ReadingId { get; set; }

    public int RoomId { get; set; }

    public string? Month { get; set; }

    public int? ElectricOld { get; set; }

    public int? ElectricNew { get; set; }

    public int? WaterOld { get; set; }

    public int? WaterNew { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Room Room { get; set; } = null!;
}
