namespace TheEmployeeAPI.Users;

public class GetUserResponse
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfilePicture { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}