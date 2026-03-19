using Microsoft.EntityFrameworkCore;
using STAYTRUST.Data;
using STAYTRUST.Models;

namespace STAYTRUST.Services;

public class FavoriteService : IFavoriteService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public FavoriteService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<bool> ToggleFavoriteAsync(int userId, int roomId)
    {
        using var context = await _factory.CreateDbContextAsync();
        var existing = await context.FavoriteRooms
            .FirstOrDefaultAsync(f => f.UserId == userId && f.RoomId == roomId);

        if (existing != null)
        {
            context.FavoriteRooms.Remove(existing);
            await context.SaveChangesAsync();
            return false; // removed
        }
        else
        {
            context.FavoriteRooms.Add(new FavoriteRoom
            {
                UserId = userId,
                RoomId = roomId,
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();
            return true; // added
        }
    }

    public async Task<List<Room>> GetUserFavoritesAsync(int userId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.FavoriteRooms
            .Where(f => f.UserId == userId)
            .Include(f => f.Room)
                .ThenInclude(r => r.RoomImages)
            .Include(f => f.Room)
                .ThenInclude(r => r.Landlord)
            .Select(f => f.Room)
            .ToListAsync();
    }

    public async Task<bool> IsFavoriteAsync(int userId, int roomId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.FavoriteRooms
            .AnyAsync(f => f.UserId == userId && f.RoomId == roomId);
    }

    public async Task<List<int>> GetUserFavoriteRoomIdsAsync(int userId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.FavoriteRooms
            .Where(f => f.UserId == userId)
            .Select(f => f.RoomId)
            .ToListAsync();
    }
}
