using Microsoft.EntityFrameworkCore;
using STAYTRUST.Data;
using STAYTRUST.Models;

namespace STAYTRUST.Services
{
    public interface IRoomService
    {
        Task<List<Room>> GetAllRoomsAsync();
        Task<Room?> GetRoomByIdAsync(int id);
        Task<List<Room>> SearchRoomsAsync(string? address, decimal? minPrice, decimal? maxPrice, double? minArea);
        Task<List<STAYTRUST.Models.DTOs.RoomMapDto>> SearchRoomsForMapAsync(decimal? minPrice, decimal? maxPrice);
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

        public async Task<List<STAYTRUST.Models.DTOs.RoomMapDto>> SearchRoomsForMapAsync(decimal? minPrice, decimal? maxPrice)
        {
            using var context = await _factory.CreateDbContextAsync();
            var query = context.Rooms.AsQueryable();

            if (minPrice.HasValue)
                query = query.Where(r => r.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(r => r.Price <= maxPrice.Value);

            var rooms = await query.ToListAsync();
            
            var dtos = new List<STAYTRUST.Models.DTOs.RoomMapDto>();
            foreach (var r in rooms)
            {
                // Consistent random based on RoomId
                var random = new System.Random(r.RoomId);
                double lat = 16.0350 + (random.NextDouble() * (16.0750 - 16.0350));
                double lng = 108.1950 + (random.NextDouble() * (108.2450 - 108.1950));

                dtos.Add(new STAYTRUST.Models.DTOs.RoomMapDto
                {
                    RoomId = r.RoomId,
                    Title = r.Title ?? "Phòng trọ",
                    Price = r.Price,
                    Latitude = lat,
                    Longitude = lng,
                    Amenities = new List<string> { "Wifi", "Điều hòa" }
                });
            }
            return dtos;
        }

        public async Task<List<Room>> SearchRoomsAsync(string? address, decimal? minPrice, decimal? maxPrice, double? minArea)
        {
            using var context = await _factory.CreateDbContextAsync();
            var query = context.Rooms
                .Include(r => r.Landlord)
                .Include(r => r.RoomImages)
                .Include(r => r.Feedbacks)
                // Removed the filter so ALL rooms show up for the demo:
                // .Where(r => r.Status != "Rented") 
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(address))
            {
                query = query.Where(r => r.Address != null && r.Address.Contains(address)
                                      || r.Title != null && r.Title.Contains(address));
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

            return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
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
