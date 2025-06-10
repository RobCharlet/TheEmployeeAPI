using FluentValidation;

namespace TheEmployeeAPI.Users;

public class RegisterRequest
{
  public string? Email { get; set; }
  public string? Password { get; set; }
  public string? ConfirmPassword { get; set; }
  public string? FirstName { get; set; }
  public string? LastName { get; set; }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
  public RegisterRequestValidator() 
  {
    RuleFor(x => x.Email)
      .NotEmpty()
      .WithMessage("Email is required.")
      .EmailAddress()
      .WithMessage("A valid email is required.");
    
    RuleFor(x => x.Password)
      .NotEmpty()
      .WithMessage("Password is required.")
      .MinimumLength(8)
      .WithMessage("Password must be at least 8 characters long.")
      .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)")
      .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, and one digit.");

  RuleFor(x => x.ConfirmPassword)
      .NotEmpty()
      .WithMessage("Password confirmation is required.")
      .Equal(x => x.Password)
      .WithMessage("Password and confirmation password do not match.");

  RuleFor(x => x.FirstName)
      .MaximumLength(100)
      .WithMessage("First name cannot exceed 100 characters.");

  RuleFor(x => x.LastName)
      .MaximumLength(100)
      .WithMessage("Last name cannot exceed 100 characters.");
  }
}
