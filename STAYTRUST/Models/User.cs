using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace STAYTRUST.Models
{
    public partial class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(15)]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(20)]
        public string Role { get; set; } = "Tenant"; // Tenant, Landlord, Admin

        public bool Status { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? AvatarUrl { get; set; }

        // Navigation properties
        public virtual UserProfile? UserProfile { get; set; }

        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();

        public virtual ICollection<RentalContract> RentalContracts { get; set; } = new List<RentalContract>();

        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}