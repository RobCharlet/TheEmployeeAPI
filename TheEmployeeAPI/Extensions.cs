using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TheEmployeeAPI;

public static class Extensions
{
  // Converts a FluentValidation ValidationResult to an ASP.NET Core ModelStateDictionary,
  // allowing validation errors to be easily returned in standard API responses.
  public static ModelStateDictionary ToModelStateDictionary(this ValidationResult validationResult)
  {
    var modelState = new ModelStateDictionary();

    foreach (var error in validationResult.Errors)
    {
      modelState.AddModelError(error.PropertyName, error.ErrorMessage);
    }

    return modelState;
  }
}