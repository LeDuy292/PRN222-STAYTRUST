using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace STAYTRUST.Models
{
    public class ForumPost
    {
        [Key]
        public int PostId { get; set; }

        public int UserId { get; set; }

        [Required]
        public string Content { get; set; } = null!;

        public string? ImageUrl { get; set; }

        public string? Category { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int LikeCount { get; set; } = 0;

        public int CommentCount { get; set; } = 0;

        public virtual User User { get; set; } = null!;

        public virtual ICollection<ForumComment> Comments { get; set; } = new List<ForumComment>();
        
        public virtual ICollection<ForumLike> Likes { get; set; } = new List<ForumLike>();
    }
}
