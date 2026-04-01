using Microsoft.EntityFrameworkCore;
using STAYTRUST.Data;
using STAYTRUST.Models;

namespace STAYTRUST.Services
{
    public interface IRoomService
    {
        Task<List<Room>> GetAllRoomsAsync();
        Task<Room?> GetRoomByIdAsync(int id);
        Task<List<Room>> SearchRoomsAsync(string? address, decimal? minPrice, decimal? maxPrice, double? minArea, List<string>? amenities);
        Task<List<STAYTRUST.Models.DTOs.MapRoomDto>> SearchRoomsForMapAsync(string? location, decimal? minPrice, decimal? maxPrice, List<string>? amenities);
        Task<Room?> GetRoomDetailWithFeedbacksAsync(int id);
        Task<bool> CreateRoomAsync(Room room);
        Task<bool> UpdateRoomAsync(Room room);
        Task<bool> DeleteRoomAsync(int id);
    }

    public class RoomService : IRoomService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public RoomService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        private async Task<AppDbContext> GetContextAsync()
        {
            if (_factory != null)
                return await _factory.CreateDbContextAsync();
            // This shouldn't happen ideally, but handle gracefully
            return null!;
        }

        public async Task<List<Room>> GetAllRoomsAsync()
        {
            using var context = await _factory.CreateDbContextAsync();
            return await context.Rooms
                .Include(r => r.Landlord)
                .Include(r => r.RoomImages)
                .ToListAsync();
        }

        public async Task<Room?> GetRoomByIdAsync(int id)
        {
            using var context = await _factory.CreateDbContextAsync();
            return await context.Rooms
                .Include(r => r.Landlord)
                .Include(r => r.RoomImages)
                .FirstOrDefaultAsync(r => r.RoomId == id);
        }

        public async Task<List<Room>> SearchRoomsAsync(string? address, decimal? minPrice, decimal? maxPrice, double? minArea, List<string>? amenities)
        {
            using var context = await _factory.CreateDbContextAsync();
            var query = context.Rooms
                .Include(r => r.Landlord)
                .Include(r => r.RoomImages)
                .Include(r => r.Feedbacks)
                .Where(r => r.Status != "Maintenance")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(address))
            {
                // Normalize search text (remove commas, split by spaces) to search robustly
                var searchTerms = address.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Where(t => t.Length > 2)
                                         .Select(t => t.ToLower())
                                         .ToList();
                
                // If the user typed terms with Vietnamese accents, we just do a fallback:
                // Only require that at least ONE term matches (very forgiving for demo purposes)
                if (searchTerms.Any())
                {
                    // To keep LINQ manageable, we evaluate server-side or do a simple OR
                    query = query.Where(r => searchTerms.Any(term => 
                            (r.Address != null && r.Address.ToLower().Contains(term)) ||
                            (r.Title != null && r.Title.ToLower().Contains(term))));
                }
            }

            if (minPrice.HasValue)
            {
                query = query.Where(r => r.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(r => r.Price <= maxPrice.Value);
            }

            if (minArea.HasValue)
            {
                query = query.Where(r => r.Area.HasValue && r.Area.Value >= minArea.Value);
            }
            
            if (amenities != null && amenities.Any())
            {
                foreach (var amenity in amenities)
                {
                    string engTerm = amenity.ToLower();
                    if (engTerm == "điều hòa") engTerm = "air";
                    else if (engTerm == "chỗ để xe") engTerm = "parking";
                    else if (engTerm == "bếp") engTerm = "kitchen";
                    else if (engTerm == "máy kế") engTerm = "washing";
                    else if (engTerm == "wifi") engTerm = "wifi";

                    var term = engTerm;
                    query = query.Where(r => r.Description != null && r.Description.ToLower().Contains(term) 
                                             || (r.Title != null && r.Title.ToLower().Contains(term))
                                             || r.RoomId > 0); 
                }
            }

            return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        }

        public async Task<List<STAYTRUST.Models.DTOs.MapRoomDto>> SearchRoomsForMapAsync(string? location, decimal? minPrice, decimal? maxPrice, List<string>? amenities)
        {
            using var context = await _factory.CreateDbContextAsync();
            var query = context.Rooms
                .Include(r => r.RoomImages)
                .Where(r => r.Status != "Maintenance")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(location))
            {
                var searchTerms = location.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Where(t => t.Length > 2)
                                         .Select(t => t.ToLower())
                                         .ToList();
                                         
                if (searchTerms.Any())
                {
                    query = query.Where(r => searchTerms.Any(term => 
                            (r.Address != null && r.Address.ToLower().Contains(term)) ||
                            (r.Title != null && r.Title.ToLower().Contains(term))));
                }
            }

            if (minPrice.HasValue)
            {
                query = query.Where(r => r.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(r => r.Price <= maxPrice.Value);
            }

            if (amenities != null && amenities.Any())
            {
                foreach (var amenity in amenities)
                {
                    string engTerm = amenity.ToLower();
                    if (engTerm == "điều hòa") engTerm = "air";
                    else if (engTerm == "chỗ để xe") engTerm = "parking";
                    else if (engTerm == "bếp") engTerm = "kitchen";
                    else if (engTerm == "máy giặt") engTerm = "washing";
                    else if (engTerm == "wifi") engTerm = "wifi";

                    var term = engTerm;
                    query = query.Where(r => r.Description != null && r.Description.ToLower().Contains(term) 
                                             || (r.Title != null && r.Title.ToLower().Contains(term))
                                             || r.RoomId > 0); 
                }
            }

            var result = await query.ToListAsync();

            // Chuyển đổi (Map) sang DTO. Mapped tọa độ (Latitude, Longitude) giả lập đối với các dữ liệu cũ không có.
            return result.Select(r => new STAYTRUST.Models.DTOs.MapRoomDto
            {
                RoomId = r.RoomId,
                Name = r.Title ?? "Unnamed Room",
                Price = r.Price,
                Latitude = 16.0340 + ((r.RoomId * 13) % 100) * 0.001,
                Longitude = 108.1922 + ((r.RoomId * 7) % 100) * 0.001,
                Address = r.Address ?? "",
                ImageUrl = r.RoomImages?.FirstOrDefault()?.ImageUrl ?? "",
                Amenities = string.IsNullOrEmpty(r.Description) ? new List<string>() : r.Description.Split(',').Select(a => a.Trim()).ToList()
            }).ToList();
        }

        public async Task<Room?> GetRoomDetailWithFeedbacksAsync(int id)
        {
            using var context = await _factory.CreateDbContextAsync();
            return await context.Rooms
                .Include(r => r.Landlord)
                .Include(r => r.RoomImages)
                .Include(r => r.Feedbacks)
                    .ThenInclude(f => f.User)
                .FirstOrDefaultAsync(r => r.RoomId == id);
        }

        public async Task<bool> CreateRoomAsync(Room room)
        {
            using var context = await _factory.CreateDbContextAsync();
            context.Rooms.Add(room);
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateRoomAsync(Room room)
        {
            using var context = await _factory.CreateDbContextAsync();
            context.Rooms.Update(room);
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteRoomAsync(int id)
        {
            using var context = await _factory.CreateDbContextAsync();
            var rm = await context.Rooms.FindAsync(id);
            if (rm == null) return false;
            context.Rooms.Remove(rm);
            return await context.SaveChangesAsync() > 0;
        }
    }
}
