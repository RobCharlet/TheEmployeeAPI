using Microsoft.AspNetCore.Mvc;
using TheEmployeeAPI.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Register EmployeeRepository service (prevent dependency injection test errors)
// It's better to not directly use the implementation for our repository and in fact 
// it's very uncommon to do so. Instead, we should use the interface.
// <IRepository<Employee> => interface
// EmployeeRepository => concrete class
builder.Services.AddSingleton<IRepository<Employee>, EmployeeRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var employeeRoute = app.MapGroup("/employees");

employeeRoute.MapGet(string.Empty, ([FromServices] IRepository<Employee> repo) => {
    var employees = repo.GetAll();
    return Results.Ok(employees.Select(employee => new GetEmployeeResponse
    {
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        Address1 = employee.Address1,
        Address2 = employee.Address2,
        City = employee.City,
        State = employee.State,
        ZipCode = employee.ZipCode,
        PhoneNumber = employee.PhoneNumber,
        Email = employee.Email
    }));
});

employeeRoute.MapGet("{id:int}", ([FromServices] IRepository<Employee> repo, int id) =>
{
    var employee = repo.GetById(id);
    if (employee == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new GetEmployeeResponse
    {
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        Address1 = employee.Address1,
        Address2 = employee.Address2,
        City = employee.City,
        State = employee.State,
        ZipCode = employee.ZipCode,
        PhoneNumber = employee.PhoneNumber,
        Email = employee.Email
    });
});

employeeRoute.MapPut("{id:int}", (
    [FromServices] IRepository<Employee> repo,
    [FromBody] UpdateEmployeeRequest employeeRequest,
    int id
    ) =>
{
    var existingEmployee = repo.GetById(id);
    if (existingEmployee == null)
    {
        return Results.NotFound();
    }
    // Update existing employee fields
    existingEmployee.Address1 = employeeRequest.Address1;
    existingEmployee.Address2 = employeeRequest.Address2;
    existingEmployee.City = employeeRequest.City;
    existingEmployee.State = employeeRequest.State;
    existingEmployee.ZipCode = employeeRequest.ZipCode;
    existingEmployee.PhoneNumber = employeeRequest.PhoneNumber;
    existingEmployee.Email = employeeRequest.Email;

    repo.Update(existingEmployee);
    // Return updated employee.
    return Results.Ok(existingEmployee);
});

employeeRoute.MapPost(string.Empty, (
    [FromServices] IRepository<Employee> repo,
    [FromBody] CreateEmployeeRequest employeeRequest) => {

    var newEmployee = new Employee {
        FirstName = employeeRequest.FirstName,
        LastName = employeeRequest.LastName,
        Address1 = employeeRequest.Address1,
        Address2 = employeeRequest.Address2,
        City = employeeRequest.City,
        State = employeeRequest.State,
        ZipCode = employeeRequest.ZipCode,
        PhoneNumber = employeeRequest.PhoneNumber,
        Email = employeeRequest.Email
    };

    repo.Create(newEmployee);

    return Results.Created($"/employees/{newEmployee.Id}", newEmployee);
});

app.UseHttpsRedirection();

app.Run();

//Expose internal types from the web app to the test project
// If not Program' est inaccessible en raison de son niveau de protection in tests.
public partial class Program { }


