using Microsoft.AspNetCore.Identity;

public class User : IdentityUser, IAuditableEntity
{
  public string? FirstName {get; set;}
  public string? LastName {get; set;}
  public string? ProfilePicture {get; set;}
  public bool IsActive {get; set;}
  public DateTime? LastLoginDate {get; set;}

   // Audit
  public string? CreatedBy { get; set; }
  public DateTime? CreatedAt { get; set; }
  public string? UpdatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }

  // Computed
  public string FullName => $"{FirstName} {LastName}".Trim();
  public  string DisplayName => !string.IsNullOrEmpty(FullName) ? FullName : Email 
    ?? UserName 
    ?? "Unknow User";
}

public interface IAuditableEntity
{
  public string? CreatedBy { get; set; }
  public DateTime? CreatedAt { get; set; }
  public string? UpdatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
}