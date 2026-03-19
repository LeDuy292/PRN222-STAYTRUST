using System;

namespace STAYTRUST.Models;

public partial class FavoriteRoom
{
    public int FavoriteId { get; set; }
    public int UserId { get; set; }
    public int RoomId { get; set; }
    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Room Room { get; set; } = null!;
}
