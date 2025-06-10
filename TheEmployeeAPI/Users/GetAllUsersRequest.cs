using FluentValidation;

namespace TheEmployeeAPI.Users;

public class GetAllUsersRequest
{
    public int? Page { get; set; }
    public int? RecordsPerPage { get; set; }
    public string? EmailContains { get; set; }
    public string? FirstNameContains { get; set; }
    public string? LastNameContains { get; set; }
    public bool? IsActive { get; set; }
}

public class GetAllUsersRequestValidator : AbstractValidator<GetAllUsersRequest>
{
    public GetAllUsersRequestValidator()
    {
        RuleFor(r => r.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be set to a positive non-zero integer.");
            
        RuleFor(r => r.RecordsPerPage)
            .GreaterThanOrEqualTo(1)
            .WithMessage("You must return at least one record.")
            .LessThanOrEqualTo(100)
            .WithMessage("You cannot return more than 100 records.");
    }
}