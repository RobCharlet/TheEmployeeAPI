public class Employee
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? SocialSecurityNumber { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }

    // One to many relationship
    public List<EmployeeBenefits> Benefits { get; set; } = new List<EmployeeBenefits>();
}

public class EmployeeBenefits
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public BenefitType BenefitType { get; set; }

    public decimal Cost { get; set; }

    // Navigation property. Not needed but good practice.
    // This property allows direct navigation from EmployeeBenefits to the related Employee object.
    // Ex:  
    // var benefit = ...;
    // var employeeName = benefit.Employee.FirstName;
    public Employee Employee { get; set; } = null!; // Remove the compiler error
}

public enum BenefitType
{
    Health,
    Dental,
    Vision
}