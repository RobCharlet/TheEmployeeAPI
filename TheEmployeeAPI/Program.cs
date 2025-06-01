using FluentValidation;
using TheEmployeeAPI.Abstractions;

var employees = new List<Employee>
{
    new Employee { Id = 1, FirstName = "John", LastName = "Doe" },
    new Employee { Id = 2, FirstName = "Jane", LastName = "Doe" }
};

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
// Standard way to return structured data describing errors from an API.
// https://datatracker.ietf.org/doc/html/rfc7807
builder.Services.AddProblemDetails();
// This allows us to request an IValidator<CreateEmployeeRequest> from the DI container and get it, no problemo.
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddControllers();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// Add controllers to the middleware.
app.MapControllers();

app.Run();

//Expose internal types from the web app to the test project
// If not Program' est inaccessible en raison de son niveau de protection in tests.
public partial class Program { }


