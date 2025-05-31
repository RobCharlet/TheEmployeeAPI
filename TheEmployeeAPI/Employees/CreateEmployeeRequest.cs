using System.ComponentModel.DataAnnotations;

public class CreateEmployeeRequest
{
    [Required(AllowEmptyStrings = false)]
    // We set the string nullable because we want to allow the caller to pass in an empty string 
    // and let the validation logic handle the invalid case, 
    // as opposed to letting the runtime throw an error.
    public string? FirstName { get; set; }
    [Required(AllowEmptyStrings = false)]
    public string? LastName { get; set; }
    public string? SocialSecurityNumber { get; set; }

    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}