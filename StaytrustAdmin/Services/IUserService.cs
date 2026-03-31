using StaytrustAdmin.Models;

namespace StaytrustAdmin.Services;

public interface IUserService
{
    Task<(List<User> Users, int TotalCount)> GetUsersAsync(
        string? search, string? role, bool? status, int page, int pageSize);

    Task<(int Total, int Admins, int Landlords, int Tenants, int Active)> GetUserStatsAsync();

    Task<User?> GetUserByIdAsync(int userId);

    Task CreateUserAsync(User user);

    Task UpdateUserAsync(User user);

    Task ToggleStatusAsync(int userId, bool newStatus);

    Task DeleteUserAsync(int userId);
}
