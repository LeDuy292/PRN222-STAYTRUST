using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using STAYTRUST.Data;
using STAYTRUST.Models;

namespace SeedRoomsApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(config.GetConnectionString("DefaultConnection"));

            using var context = new AppDbContext(optionsBuilder.Options);

            try
            {
                Console.WriteLine("Seeding data...");

                // 1. Ensure Landlord exists
                var landlord = await context.Users.FirstOrDefaultAsync(u => u.Role == "Landlord");
                if (landlord == null)
                {
                    landlord = new User
                    {
                        FullName = "Test Landlord",
                        UserName = "landlord_test",
                        Email = "landlord@staytrust.com",
                        Phone = "0987654321",
                        Password = "Password123!", // Note: In a real app, this should be hashed
                        Role = "Landlord",
                        Status = true,
                        CreatedAt = DateTime.Now
                    };
                    context.Users.Add(landlord);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"Created new Landlord: {landlord.UserName}");
                }
                else
                {
                    Console.WriteLine($"Using existing Landlord: {landlord.UserName} (ID: {landlord.UserId})");
                }

                // 2. Sample Rooms data
                var rooms = new List<Room>
                {
                    new Room
                    {
                        LandlordId = landlord.UserId,
                        Title = "Phòng trọ cao cấp Quận 1 - Full nội thất",
                        Description = "Phòng trọ mới xây, đầy đủ tiện nghi: máy lạnh, tủ lạnh, giường nệm. Gần chợ và trung tâm thương mại.",
                        Price = 5500000,
                        Deposit = 5500000,
                        Area = 25.5,
                        Address = "123 Đề Thám, Phường Cô Giang, Quận 1, TP.HCM",
                        Status = "Available",
                        Bedrooms = 1,
                        Bathrooms = 1,
                        Floor = 3,
                        BuildingFloors = 5,
                        YearBuilt = 2023,
                        Type = "Studio",
                        Verified = true,
                        Featured = true,
                        CreatedAt = DateTime.Now
                    },
                    new Room
                    {
                        LandlordId = landlord.UserId,
                        Title = "Căn hộ mini giá rẻ gần Đại học Bách Khoa",
                        Description = "Gần trạm xe buýt, yên tĩnh, an ninh 24/7. Phù hợp cho sinh viên hoặc người đi làm.",
                        Price = 3200000,
                        Deposit = 3000000,
                        Area = 18.0,
                        Address = "45/10 Lý Thường Kiệt, Quận 10, TP.HCM",
                        Status = "Available",
                        Bedrooms = 1,
                        Bathrooms = 1,
                        Floor = 1,
                        BuildingFloors = 4,
                        YearBuilt = 2021,
                        Type = "Mini Apartment",
                        Verified = false,
                        Featured = false,
                        CreatedAt = DateTime.Now
                    },
                    new Room
                    {
                        LandlordId = landlord.UserId,
                        Title = "Phòng trọ ban công rộng - Thoáng mát Quận 7",
                        Description = "View cực đẹp, thoáng mát, có ban công riêng. Khu vực dân cư cao cấp.",
                        Price = 4500000,
                        Deposit = 4500000,
                        Area = 22.0,
                        Address = "789 Huỳnh Tấn Phát, Quận 7, TP.HCM",
                        Status = "Pending",
                        Bedrooms = 1,
                        Bathrooms = 1,
                        Floor = 4,
                        BuildingFloors = 6,
                        YearBuilt = 2022,
                        Type = "Standard",
                        Verified = true,
                        Featured = false,
                        CreatedAt = DateTime.Now
                    }
                };

                foreach (var room in rooms)
                {
                    var existing = await context.Rooms.FirstOrDefaultAsync(r => r.Title == room.Title);
                    if (existing == null)
                    {
                        context.Rooms.Add(room);
                        await context.SaveChangesAsync();
                        
                        // Add a sample image for each room
                        context.RoomImages.Add(new RoomImage
                        {
                            RoomId = room.RoomId,
                            ImageUrl = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=1200",
                            Approved = true
                        });
                        
                        Console.WriteLine($"Added room: {room.Title}");
                    }
                }

                await context.SaveChangesAsync();
                Console.WriteLine("\nSeeding completed successfully!");
                Console.WriteLine($"Summary: {rooms.Count} potential rooms checked/added.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
        }
    }
}
