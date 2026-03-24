using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace STAYTRUST.Models
{
    public class ForumComment
    {
        [Key]
        public int CommentId { get; set; }

        public int PostId { get; set; }

        public int UserId { get; set; }

        [Required]
        public string Content { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ForumPost Post { get; set; } = null!;

        public virtual User User { get; set; } = null!;
    }
}
