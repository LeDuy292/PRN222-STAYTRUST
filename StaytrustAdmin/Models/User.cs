namespace StaytrustAdmin.Models;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Tenant"; // Tenant | Landlord | Admin
    public bool Status { get; set; } = true;      // true = Active
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // From JOIN with UserProfiles
    public string? AvatarUrl { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? IdentityNumber { get; set; }
    public string? Address { get; set; }

    // Computed helpers
    public string Initials => string.Join("", FullName.Split(' ')
        .Where(w => w.Length > 0)
        .Take(2)
        .Select(w => w[0]));

    public string StatusLabel => Status ? "Active" : "Inactive";
}
