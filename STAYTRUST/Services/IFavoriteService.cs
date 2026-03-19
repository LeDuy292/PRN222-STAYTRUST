using STAYTRUST.Models;

namespace STAYTRUST.Services;

public interface IFavoriteService
{
    Task<bool> ToggleFavoriteAsync(int userId, int roomId);
    Task<List<Room>> GetUserFavoritesAsync(int userId);
    Task<bool> IsFavoriteAsync(int userId, int roomId);
    Task<List<int>> GetUserFavoriteRoomIdsAsync(int userId);
}
