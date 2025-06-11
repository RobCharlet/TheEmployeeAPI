namespace TheEmployeeAPI.Users;

public class PasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IEnumerable<string>? Errors { get; set; }
} 