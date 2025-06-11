using FluentValidation;

namespace TheEmployeeAPI.Users;

public class ResetPasswordRequest
{
    public string? NewPassword { get; set; }
    public string? ConfirmNewPassword { get; set; }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.")
            .MinimumLength(6)
            .WithMessage("New password must be at least 6 characters long.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)")
            .WithMessage("New password must contain at least one uppercase letter, one lowercase letter, and one digit.");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty()
            .WithMessage("Password confirmation is required.")
            .Equal(x => x.NewPassword)
            .WithMessage("New password and confirmation password do not match.");
    }
} 