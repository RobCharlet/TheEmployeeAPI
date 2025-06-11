namespace TheEmployeeAPI.Users;

public class AuthResponse
{
    public bool IsAuthenticated { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public string Message { get; set; } = string.Empty;
}