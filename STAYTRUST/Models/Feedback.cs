using System;
using System.Collections.Generic;

namespace STAYTRUST.Models;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int RoomId { get; set; }

    public int UserId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Room Room { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
