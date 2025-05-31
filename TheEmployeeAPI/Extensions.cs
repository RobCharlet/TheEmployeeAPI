using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace TheEmployeeAPI;

/**
 * Converts a List<ValidationResult> to a ValidationProblemDetails object.
 */
public static class Extensions
{
  // Extension method to map validation results to the API-friendly problem details format.
  public static ValidationProblemDetails ToValidationProblemDetails(this List<ValidationResult> validationResults)
  {
    var problemDetails = new ValidationProblemDetails();

    // For each validation error, add messages to the corresponding property key
    foreach (var validationResult in validationResults)
    {
      foreach (var memberName in validationResult.MemberNames)
      {
        if (problemDetails.Errors.ContainsKey(memberName))
        {
          // Add the error message to the existing array
          problemDetails.Errors[memberName] = problemDetails
            .Errors[memberName]
            .Concat([validationResult.ErrorMessage])
            .ToArray()!;
        }
        else
        {
          // Create a new entry for this property with the error message
          problemDetails.Errors[memberName] = new List<string> { validationResult.ErrorMessage! }.ToArray();
        }
      }
    }

    return problemDetails;
  }
}

#region Examples
/*
Example 1: Single ValidationResult error for "FirstName"

Input:
  var validationResults = new List<ValidationResult>
  {
      new ValidationResult("First name is required.", new[] { "FirstName" })
  };

Output (problemDetails.Errors):
{
    "FirstName": [ "First name is required." ]
}

------------------------------------------

Example 2: Two ValidationResult errors, one for "FirstName", one for "Email" (with two errors)

Input:
  var validationResults = new List<ValidationResult>
  {
      new ValidationResult("First name is required.", new[] { "FirstName" }),
      new ValidationResult("Email is required.", new[] { "Email" }),
      new ValidationResult("Email must be a valid email address.", new[] { "Email" })
  };

Output (problemDetails.Errors):
{
    "FirstName": [ "First name is required." ],
    "Email": [
        "Email is required.",
        "Email must be a valid email address."
    ]
}
*/
#endregion