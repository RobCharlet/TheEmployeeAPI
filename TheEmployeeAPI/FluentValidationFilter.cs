using FluentValidation;
using FluentValidation.Results;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

public class FluentValidationFilter : IAsyncActionFilter
{
  private readonly IServiceProvider _serviceProvider;
  private readonly ProblemDetailsFactory _problemDetailsFactory;

  public FluentValidationFilter(
    IServiceProvider serviceProvider,
    ProblemDetailsFactory problemDetailsFactory
  )
  {
    _serviceProvider = serviceProvider;
    _problemDetailsFactory = problemDetailsFactory;
  }

  // This method is called before and after the action executes
  public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
  {
    // Iterate through each parameter of the action
    foreach (var parameter in context.ActionDescriptor.Parameters)
    {
      // Try to get the value of the parameter from the action arguments
      if (context.ActionArguments.TryGetValue(parameter.Name, out var argumentValue) && argumentValue != null)
      {
        // Get the type of the argument and try to resolve a validator for it

        // Use reflection to get the runtime type of the argument
        var argumentType = argumentValue.GetType();
        // Use reflection to construct the closed generic type IValidator<argumentType>
        // For example, if argumentType is Employee, this becomes IValidator<Employee>
        var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
        // Use the service provider to get an instance of the validator for this type
        // This is also done dynamically using the constructed type
        var validator = _serviceProvider.GetService(validatorType) as IValidator;

        if (validator != null)
        {
          // Validate the argument using FluentValidation
          ValidationResult validationResult = await validator.ValidateAsync(new ValidationContext<object>(argumentValue));

          if (!validationResult.IsValid)
          {
            // If validation fails, add errors to ModelState
            validationResult.AddToModelState(context.ModelState);
            // Create a ProblemDetails response for validation errors
            var problemDetails = _problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext, context.ModelState);
            // Return a 400 BadRequest with the problem details and stop action execution
            context.Result = new BadRequestObjectResult(problemDetails);

            return;
          }
        }
      }
    }
    // If all validations pass, continue to the next action/middleware
    await next();
  }

  // Not used, but required by the interface
  public void onActionExecuted(ActionExecutedContext context) {}

}