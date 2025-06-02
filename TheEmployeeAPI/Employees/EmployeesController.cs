using Microsoft.AspNetCore.Mvc;
using TheEmployeeAPI.Abstractions;

namespace TheEmployeeAPI.Employees;

public class EmployeesController : BaseController
{
  private readonly IRepository<Employee> _repository;
  private readonly ILogger<EmployeesController> _logger;

  public EmployeesController(
    IRepository<Employee> repository,
    ILogger<EmployeesController> logger
  )
  {
    _repository = repository;
    _logger = logger;
  }

  // XML Comments:
  /// <summary>
  /// Get all employees.
  /// </summary>
  /// <returns>An array of all employees.</returns>
  [HttpGet]
  [ProducesResponseType(typeof(IEnumerable<GetEmployeeResponse>), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public IActionResult GetAllEmployees()
  {
    var employees = _repository.GetAll().Select(EmployeeToGetEmployeeResponse);
    return Ok(employees);
  }

  /// <summary>
  /// Gets an employee by ID.
  /// </summary>
  /// <param name="id">The ID of the employee.</param>
  /// <returns>The single employee record.</returns>
  [HttpGet("{id}")]
  [ProducesResponseType(typeof(GetEmployeeResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public IActionResult GetEmployeeById([FromRoute] int id)
  {
    var employee = _repository.GetById(id);
    if (employee == null)
    {
      return NotFound();
    }
    return Ok(EmployeeToGetEmployeeResponse(employee));
  }

  /// <summary>
  /// Creates a new employee.
  /// </summary>
  /// <param name="employeeRequest">The employee to be created.</param>
  /// <returns>A link to the employee that was created.</returns>
  [HttpPost]
  [ProducesResponseType(typeof(GetEmployeeResponse), StatusCodes.Status201Created)]
  [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> CreateEmployee(
    [FromBody] CreateEmployeeRequest employeeRequest
  )
  {
    // Replaced with the filter FluentValidationFilter.
    // Validate the incoming employee request using the validator generator
    // defined in the controller.
    // var validationResults = await ValidateAsync(employeeRequest);

    // // If validation fails, return a structured validation problem response.
    // if (!validationResults.IsValid)
    // {
    //   // Use custom extension ToModelStateDictionary.
    //   return ValidationProblem(validationResults.ToModelStateDictionary());
    // }

    await Task.CompletedTask;   //just avoided a compiler error for now
    var newEmployee = new Employee
    {
      // We know better than the compiler that FirstName and LastName can't be null.
      FirstName = employeeRequest.FirstName!,
      LastName = employeeRequest.LastName!,
      Address1 = employeeRequest.Address1,
      Address2 = employeeRequest.Address2,
      City = employeeRequest.City,
      State = employeeRequest.State,
      ZipCode = employeeRequest.ZipCode,
      PhoneNumber = employeeRequest.PhoneNumber,
      Email = employeeRequest.Email
    };

    _repository.Create(newEmployee);

    /*
      Generate best practice REST response for created resource
      ex.
        HTTP/1.1 201 Created
        Location: /employees/3
        Content-Type: application/json

        {
          "id": 3,
          "firstName": "Anna",
          "lastName": "Smith",
          ...
        }
    */
    return CreatedAtAction(nameof(GetEmployeeById), new { id = newEmployee.Id }, newEmployee);
  }

  /// <summary>
  /// Updates an employee.
  /// </summary>
  /// <param name="id">The ID of the employee to update.</param>
  /// <param name="updateEmployeeRequest">The employee data to update.</param>
  /// <returns></returns>
  [HttpPut("{id}")]
  [ProducesResponseType(typeof(GetEmployeeResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public IActionResult UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest updateEmployeeRequest)
  {
    _logger.LogInformation("Updating employee with ID: {EmployeeId}", id);

    var existingEmployee = _repository.GetById(id);
    if (existingEmployee == null)
    {
      _logger.LogInformation("Employee with ID: {EmployeeId} not found", id);
      return NotFound();
    }

    // Update existing employee fields
    existingEmployee.Address1 = updateEmployeeRequest.Address1;
    existingEmployee.Address2 = updateEmployeeRequest.Address2;
    existingEmployee.City = updateEmployeeRequest.City;
    existingEmployee.State = updateEmployeeRequest.State;
    existingEmployee.ZipCode = updateEmployeeRequest.ZipCode;
    existingEmployee.PhoneNumber = updateEmployeeRequest.PhoneNumber;
    existingEmployee.Email = updateEmployeeRequest.Email;

    _repository.Update(existingEmployee);
    _logger.LogInformation("Employee with ID: {EmployeeId} successfully updated", id);
    return Ok(existingEmployee);
  }

  /// <summary>
  /// Gets the benefits for an employee.
  /// </summary>
  /// <param name="employeeId">The ID to get the benefits for.</param>
  /// <returns>The single employee record.</returns>
  [HttpGet("{employeeId}/benefits")]
  [ProducesResponseType(typeof(IEnumerable<GetEmployeeResponseEmployeeBenefit>), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public IActionResult GetBenefitsForEmployee(int employeeId)
  {
    var employee = _repository.GetById(employeeId);

    if (employee == null)
    {
      return NotFound();
    }

    return Ok(employee.Benefits.Select(BenefitToBenefitResponse));
  }

  private static GetEmployeeResponse EmployeeToGetEmployeeResponse(Employee employee)
  {
    return new GetEmployeeResponse
    {
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        Address1 = employee.Address1,
        Address2 = employee.Address2,
        City = employee.City,
        State = employee.State,
        ZipCode = employee.ZipCode,
        PhoneNumber = employee.PhoneNumber,
        Email = employee.Email,
        Benefits = employee.Benefits.Select(BenefitToBenefitResponse).ToList()
    };
}

private static GetEmployeeResponseEmployeeBenefit BenefitToBenefitResponse(EmployeeBenefits benefit)
{
    return new GetEmployeeResponseEmployeeBenefit
    {
        Id = benefit.Id,
        EmployeeId = benefit.EmployeeId,
        BenefitType = benefit.BenefitType,
        Cost = benefit.Cost
    };
}
}
