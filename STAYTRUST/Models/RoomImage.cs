using System;
using System.Collections.Generic;

namespace STAYTRUST.Models;

public partial class RoomImage
{
    public int ImageId { get; set; }

    public int RoomId { get; set; }

    public string? ImageUrl { get; set; }

    public bool? Approved { get; set; }

    public virtual Room Room { get; set; } = null!;
}
