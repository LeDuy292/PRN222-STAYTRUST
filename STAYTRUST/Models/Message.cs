using System;

namespace STAYTRUST.Models;

public partial class Message
{
    public int MessageId { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public int? RoomId { get; set; }
    public string Content { get; set; } = null!;
    public bool? IsRead { get; set; }
    public DateTime? CreatedAt { get; set; }

    public virtual User Sender { get; set; } = null!;
    public virtual User Receiver { get; set; } = null!;
    public virtual Room? Room { get; set; }
}
