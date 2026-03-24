using System;
using System.ComponentModel.DataAnnotations;

namespace STAYTRUST.Models
{
    public class ForumLike
    {
        [Key]
        public int LikeId { get; set; }

        public int PostId { get; set; }

        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ForumPost Post { get; set; } = null!;

        public virtual User User { get; set; } = null!;
    }
}
