using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace TheEmployeeAPI;
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
    private readonly AppDbContext _dbContext;

    public UpdateEmployeeRequestValidator(
            IHttpContextAccessor httpContextAccessor,
            AppDbContext dbContext
        )
        {
            // Get the current HTTP context from the accessor cause we are outside a Controller
            _httpContext = httpContextAccessor.HttpContext!;
            _dbContext = dbContext;


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
        var employee =  await _dbContext.Employees.SingleOrDefaultAsync(e => e.Id == id);

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