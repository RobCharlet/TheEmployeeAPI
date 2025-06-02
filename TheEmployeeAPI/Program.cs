using FluentValidation;
using TheEmployeeAPI.Abstractions;

var employees = new List<Employee>
{
    new Employee {
        Id = 1,
        FirstName = "John",
        LastName = "Doe",
        Benefits = new List<EmployeeBenefits>
            {
                new EmployeeBenefits { BenefitType = BenefitType.Health, Cost = 100 },
                new EmployeeBenefits { BenefitType = BenefitType.Dental, Cost = 50 }
            }
    },
    new Employee { Id = 2, FirstName = "Jane", LastName = "Doe" }
};

var employeeRepository = new EmployeeRepository();

foreach (var employee in employees) {
    employeeRepository.Create(employee);
}
    
    // repo.Create(new Employee
// {
//     FirstName = "John",
//     LastName = "Doe",
//     Address1 = "123 Main St",
//     Benefits = new List<EmployeeBenefits>
//     {
//         new EmployeeBenefits { BenefitType = BenefitType.Health, Cost = 100 },
//         new EmployeeBenefits { BenefitType = BenefitType.Dental, Cost = 50 }
//     }
// });

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Enable XML comments
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "TheEmployeeAPI.xml"));
});
// Register EmployeeRepository service (prevent dependency injection test errors)
// It's better to not directly use the implementation for our repository and in fact 
// it's very uncommon to do so. Instead, we should use the interface.
// <IRepository<Employee> => interface
// EmployeeRepository => concrete class
builder.Services.AddSingleton<IRepository<Employee>>(employeeRepository);
// Standard way to return structured data describing errors from an API.
// https://datatracker.ietf.org/doc/html/rfc7807
builder.Services.AddProblemDetails();
// This allows us to request an IValidator<CreateEmployeeRequest> from the DI container and get it, no problemo.
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationFilter>();
});
builder.Services.AddHttpContextAccessor();

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


