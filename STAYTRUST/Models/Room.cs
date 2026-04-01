using System;
using System.Collections.Generic;

namespace STAYTRUST.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public int LandlordId { get; set; }
    public int? ManagedByUserId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal Deposit { get; set; }

    public double? Area { get; set; }

    public string? Address { get; set; }

    public string? Status { get; set; }

    public string? Image360Url { get; set; }

    public int Bedrooms { get; set; }

    public int Bathrooms { get; set; }

    public int Floor { get; set; }

    public int BuildingFloors { get; set; }

    public int YearBuilt { get; set; }

    public string? Type { get; set; }

    public bool Verified { get; set; }

    public bool Featured { get; set; }

    public double Rating { get; set; }

    public int Reviews { get; set; }

    public int Views { get; set; }

    public int Inquiries { get; set; }


    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual User Landlord { get; set; } = null!;

    public virtual ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();

    public virtual ICollection<UtilityRate> UtilityRates { get; set; } = new List<UtilityRate>();

    public virtual ICollection<RentalContract> RentalContracts { get; set; } = new List<RentalContract>();

    public virtual ICollection<RoomImage> RoomImages { get; set; } = new List<RoomImage>();

    public virtual ICollection<FavoriteRoom> FavoriteRooms { get; set; } = new List<FavoriteRoom>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}
