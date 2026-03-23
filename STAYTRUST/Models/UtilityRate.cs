using System;

namespace STAYTRUST.Models;

public class UtilityRate
{
    public int RateId { get; set; }

    public int RoomId { get; set; }

    public decimal ElectricPrice { get; set; } = 3500;

    public decimal WaterPrice { get; set; } = 12000;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public virtual Room Room { get; set; } = null!;
}
