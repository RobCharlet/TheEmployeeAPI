using FluentValidation;

namespace TheEmployeeAPI.Users;

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfilePicture { get; set; }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .MaximumLength(100)
            .WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .MaximumLength(100)
            .WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.ProfilePicture)
            .MaximumLength(500)
            .WithMessage("Profile picture URL cannot exceed 500 characters.")
            .Must(BeValidUrlOrEmpty)
            .WithMessage("Profile picture must be a valid URL.");
    }

    private bool BeValidUrlOrEmpty(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;
            
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}