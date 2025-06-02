using FluentValidation;
using TheEmployeeAPI.Abstractions;


public class UpdateEmployeeRequest
{
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    private readonly HttpContext _httpContext;
    private readonly IRepository<Employee> _repository;

    public UpdateEmployeeRequestValidator(
        IHttpContextAccessor httpContextAccessor,
        IRepository<Employee> repository
    )
    {
        // Get the current HTTP context from the accessor cause we are outside a Controller
        this._httpContext = httpContextAccessor.HttpContext!;
        // "this" is use to remove any ambiguity between a field and parameter with the same name.
        // Not mandatory.
        this._repository = repository;

        RuleFor(x => x.Address1)
            // This rule ensures that if the employee already has an Address1 value,
            // it cannot be replaced with an empty or whitespace value during update.
            .MustAsync(NotBeEmptyIfItIsSetOnEmployeeAlreadyAsync)
            .WithMessage("Address1 must not be empty as an address was already set on the employee.");
    }

    private async Task<bool> NotBeEmptyIfItIsSetOnEmployeeAlreadyAsync(
        string? address,
        CancellationToken token
    )
    {
        await Task.CompletedTask; //again, we'll not make this async for now!

        // Read the id from the request.
        var id = Convert.ToInt32(_httpContext.Request.RouteValues["id"]);
        var employee = _repository.GetById(id);

        if (employee == null) {
            return true;
        }

        if (employee.Address1 != null && string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        return true;
    }
    
    
}