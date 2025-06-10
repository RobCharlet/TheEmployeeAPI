using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TheEmployeeAPI.Employees;

public class EmployeesController : BaseController
{
  private readonly ILogger<EmployeesController> _logger;
  private readonly AppDbContext _dbContext;

  public EmployeesController(
    ILogger<EmployeesController> logger,
    AppDbContext dbContext
  )
  {
    _logger = logger;
    _dbContext = dbContext;
  }

  // XML Comments:
  /// <summary>
  /// Get all employees.
  /// </summary>
  /// <returns>An array of all employees.</returns>
  [HttpGet]
  [ProducesResponseType(typeof(IEnumerable<GetEmployeeResponse>), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  // [FromQuery] will instruct ASP.NET Core to put request parameters to the query
  // Which will allow filtering and pagination.
  public async Task<IActionResult> GetAllEmployees([FromQuery] GetAllEmployeesRequest? request)
  {
    int page = request?.Page ?? 1;
    int numberOfRecordsPerPage = request?.RecordsPerPage ?? 100;

    // We are constructing the QUERY
    IQueryable<Employee> query = _dbContext.Employees
      // EF Core won't pull subrequested Benefits back unless we specifically request them
      // Skip previous pages if page > 1
      .Skip((page - 1) * numberOfRecordsPerPage)
      // Take only the needed records
      .Take(numberOfRecordsPerPage);

    // Filters by FirstName and LastName
    if (request != null)
    {
      if (!string.IsNullOrWhiteSpace(request.FirstNameContains))
      {
        query = query.Where(e => e.FirstName.Contains(request.FirstNameContains));
      }
      if (!string.IsNullOrWhiteSpace(request.LastNameContains))
      {
        query = query.Where(e => e.LastName.Contains(request.LastNameContains));
      }
    }

    var employees = await query.ToArrayAsync();

    return Ok(employees.Select(EmployeeToGetEmployeeResponse));
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
  public async Task<IActionResult> GetEmployeeById([FromRoute] int id)
  {
    var employee = await _dbContext.Employees.SingleOrDefaultAsync(e => e.Id == id);

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

    _dbContext.Employees.Add(newEmployee);
    await _dbContext.SaveChangesAsync();

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
  /// Gets the benefits for an employee.
  /// </summary>
  /// <param name="employeeId">The ID to get the benefits for.</param>
  /// <returns>The benefits for that employee.</returns>
  [HttpGet("{employeeId}/benefits")]
  [ProducesResponseType(typeof(IEnumerable<GetEmployeeResponseEmployeeBenefit>), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> GetBenefitsForEmployee(int employeeId)
  {
    var employee = await _dbContext.Employees
      .Include(e => e.Benefits) // get employeeBenefits
      .ThenInclude(e => e.Benefit) // get benefits from employeeBenefits
      .SingleOrDefaultAsync(e => e.Id == employeeId);

      /* Equivalent to : 
      
      SELECT e.*, eb.Id AS EmployeeBenefitId, eb.CostToEmployee, b.Id AS BenefitId, b.Name, b.Description, b.BaseCost
      FROM Employees e
      LEFT JOIN EmployeeBenefits eb ON eb.EmployeeId = e.Id
      LEFT JOIN Benefits b ON b.Id = eb.BenefitId
      WHERE e.Id = @employeeId;*/

    if (employee == null)
    {
      return NotFound();
    }

    var benefits = employee.Benefits.Select(b => new GetEmployeeResponseEmployeeBenefit
    {
      Id = b.Id,
      Name = b.Benefit.Name,
      Description = b.Benefit.Description,
      Cost = b.CostToEmployee ?? b.Benefit.BaseCost
    });

    return Ok(benefits);
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
  public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest updateEmployeeRequest)
  {
    _logger.LogInformation("Updating employee with ID: {EmployeeId}", id);

    var existingEmployee = await _dbContext.Employees
      // .Employees.FindAsync(id);
      // We can turn on ChangeTracking for a specific query
      .AsTracking()
      .SingleOrDefaultAsync(e => e.Id == id);

    // We can also turn off the tracking for a specific query
    // ChangeTracking is turned on to get a little perf for read only
    //var existingEmployee = await _dbContext.Employees.AsNoTracking();


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

    try
    {
      // Not needed if EF Core ChangeTracker is not disabled or
      // tracking is enabled locally with .AsTracking().
      // EF Core will track the state of the objects that are added to the context
      // https://schneidenbach.github.io/building-apis-with-csharp-and-aspnet-core/lessons/entity-framework-core/adding-and-removing-objects-from-your-database
      //_dbContext.Entry(existingEmployee).State = EntityState.Modified;

      await _dbContext.SaveChangesAsync();
      _logger.LogInformation("Employee with ID: {EmployeeId} successfully updated", id);

      return Ok(existingEmployee);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while updating employee with ID: {EmployeeId}", id);
      return StatusCode(500, "An error occurred while updating the employee");
    }
  }

  /// <summary>
  /// Delete an employee.
  /// </summary>
  /// <param name="id">The ID of the employee to delete.</param>
  /// <returns></returns>
  [HttpDelete("{id}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> DeleteEmployee(int id)
  {
    var employee = await _dbContext.Employees.FindAsync(id);

    if (employee == null)
    {
      return NotFound();
    }

    _dbContext.Employees.Remove(employee);
    await _dbContext.SaveChangesAsync();

    return NoContent();
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
    };
  }
}
