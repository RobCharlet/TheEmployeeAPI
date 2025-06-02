
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace TheEmployeeAPI;

[ApiController]
[Route("[controller]")]
// Will always return "application/json" response.
[Produces("application/json")]
public class BaseController : Controller
{

  // We don't want to ineject a validator service for each
  // request type like IValidator<CreateEmployeeRequest> createValidator,
  // IValidator<UpdateEmployeeRequest> updateValidator
  // used in one endpoint.
  // So we create a function with generic type to generate one on the go.
  protected async Task<ValidationResult> ValidateAsync<T>(T instance)
  {
    var validator = HttpContext.RequestServices.GetService<IValidator<T>>();
    if (validator == null)
    {
      throw new ArgumentException($"No validator found for {typeof(T).Name}");
    }
    var result = await validator.ValidateAsync(instance);
    return result;
  }
}
