using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheEmployeeAPI;

namespace TheEmployeeAPI.Controllers;

public class EmployeesController : Controller
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

  // [FromQuery] will instruct ASP.NET Core to put request parameters to the query
  // Which will allow filtering and pagination.
  public async Task<IActionResult> Index([FromQuery] GetAllEmployeesRequest? request)
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

    var employees = await query.ToListAsync();

    ViewBag.CurrentPage = page;
    ViewBag.FirstNameFilter = request?.FirstNameContains;
    ViewBag.LastNameFilter = request?.LastNameContains;

    return View(employees);
  }


  public async Task<IActionResult> Details([FromRoute] int id)
  {
    var employee = await _dbContext.Employees
    // Left join on EmployeeBenefits
    .Include(e => e.EmployeeBenefits)
    .ThenInclude(eb => eb.Benefit)
    .SingleOrDefaultAsync(e => e.Id == id);

    if (employee == null)
    {
      return NotFound();
    }
    
    return View(employee);
  }

  [HttpGet]
  public async Task<IActionResult> Create()
  {
    var benefits = await _dbContext.Benefits.ToListAsync();
    ViewBag.Benefits = benefits;
      
    return View();
  }

  [HttpPost]
  //[Authorize]
  [ValidateAntiForgeryToken] // Prevents CSRF attacks
  public async Task<IActionResult> Create(CreateEmployeeRequest model)
  {
    // FluentValidation has automatically validated and populated ModelState
    if (ModelState.IsValid)
    {
      var newEmployee = new Employee
      {
        FirstName = model.FirstName!,
        LastName = model.LastName!,
        Address1 = model.Address1,
        Address2 = model.Address2,
        City = model.City,
        State = model.State,
        ZipCode = model.ZipCode,
        PhoneNumber = model.PhoneNumber,
        Email = model.Email,
      };

      _dbContext.Employees.Add(newEmployee);
      await _dbContext.SaveChangesAsync();

      // Add employee benefits
      foreach(var benefitId in model.SelectedBenefitsIds) {
        var employeeBenefit = new EmployeeBenefit {
          EmployeeId = newEmployee.Id,
          BenefitId = benefitId,
        };
        _dbContext.EmployeeBenefits.Add(employeeBenefit);
      }
      await _dbContext.SaveChangesAsync();

      TempData["Success"] = "Employé créé avec succès !";
      return RedirectToAction(nameof(Index));
    }

    // Load benefit listing
    var benefits = await _dbContext.Benefits.ToListAsync();
    ViewBag.Benefits = benefits;

    return View(model);
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
      .Include(e => e.EmployeeBenefits) // get employeeBenefits
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

    var benefits = employee.EmployeeBenefits.Select(b => new GetEmployeeResponseEmployeeBenefit
    {
      Id = b.Id,
      Name = b.Benefit.Name,
      Description = b.Benefit.Description,
      Cost = b.CostToEmployee ?? b.Benefit.BaseCost
    });

    return Ok(benefits);
  }

  // /Employees/Edit/{id}
  [HttpGet]
  //[Authorize]
  public async Task<IActionResult> Edit([FromRoute] int id)
  {
    var employee = await _dbContext.Employees.SingleOrDefaultAsync(e => e.Id == id);
    
    if (employee == null) {
      return NotFound();
    }

    // Creates the editable model form
    var updateEmployeeRequest = new UpdateEmployeeRequest
    {
      // Pre-fill edit form
      // only editable fields
      Address1 = employee.Address1,
      Address2 = employee.Address2,
      City = employee.City,
      State = employee.State,
      ZipCode = employee.ZipCode,
      PhoneNumber = employee.PhoneNumber,
      Email = employee.Email
    };
    
    // Send full employee to the view display
    ViewBag.Employee = employee;

    // Send benefits to the view display
    var benefits = await _dbContext.Benefits.ToListAsync();
    ViewBag.Benefits = benefits;
    
    // Return updateEmployeeRequest model to the view
    return View(updateEmployeeRequest);
  }


  [HttpPost]
  // [Authorize]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Edit ([FromRoute] int id, UpdateEmployeeRequest model)
  {
    // The id from the route is the source of truth.
    var existingEmployee = await _dbContext.Employees
        .Include(e => e.EmployeeBenefits) // include existing benefits
        .SingleOrDefaultAsync(e => e.Id == id);

    if (existingEmployee == null)
    {
        return NotFound();
    }

    if (ModelState.IsValid) {
      // Update existing employee fields
      existingEmployee.Address1 = model.Address1;
      existingEmployee.Address2 = model.Address2;
      existingEmployee.City = model.City;
      existingEmployee.State = model.State;
      existingEmployee.ZipCode = model.ZipCode;
      existingEmployee.PhoneNumber = model.PhoneNumber;
      existingEmployee.Email = model.Email;

      // Remove old benefits
      _dbContext.EmployeeBenefits.RemoveRange(existingEmployee.EmployeeBenefits);

      // Add new employee benefits
      foreach(var benefitId in model.SelectedBenefitsIds) {
        var employeeBenefit = new EmployeeBenefit {
          EmployeeId = existingEmployee.Id,
          BenefitId = benefitId,
        };
        _dbContext.EmployeeBenefits.Add(employeeBenefit);
      }
      await _dbContext.SaveChangesAsync();

      TempData["Success"] = "Employé modifié avec succès !";

      return RedirectToAction(nameof(Details), new { id = id });
    }

    var employee = await _dbContext.Employees.SingleOrDefaultAsync(e => e.Id == id);
    ViewBag.Employee = employee;

    var benefits = await _dbContext.Benefits.ToListAsync();
    ViewBag.Benefits = benefits;

    return View(model);
  }


  [HttpPost]
  //[Authorize]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Delete([FromRoute] int id)
  {
    var employee = await _dbContext.Employees
    .AsTracking()
    .SingleOrDefaultAsync(e => e.Id == id);

    if (employee == null)
    {
      return NotFound();
    }

    _dbContext.Employees.Remove(employee);

    await _dbContext.SaveChangesAsync();

    TempData["Success"] = "Employé supprimé avec succès !";
    return RedirectToAction(nameof(Index));
  }
}
