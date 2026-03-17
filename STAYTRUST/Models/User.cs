using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace STAYTRUST.Models
{
    public int UserId { get; set; }

        [StringLength(20)]
        public string Role { get; set; } = "Tenant"; // Tenant, Landlord, Admin

    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Role { get; set; }

    public bool? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<RentalContract> RentalContracts { get; set; } = new List<RentalContract>();

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();

    public virtual UserProfile? UserProfile { get; set; }
}
