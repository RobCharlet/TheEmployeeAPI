using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using TheEmployeeAPI.Employees;
using TheEmployeeAPI.Users;

namespace TheEmployeeAPI.Tests;

public class EmployeesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly int _employeeId = 1;
    private readonly CustomWebApplicationFactory _factory;

    public EmployeesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllEmployees_ReturnsOkResult()
    {
        HttpClient client = _factory.CreateClient();
        var response = await client.GetAsync("/employees");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetAllEmployees_WithFilter_ReturnsOneResult()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/employees?FirstNameContains=John");

        response.EnsureSuccessStatusCode();

        var employees = await response.Content.ReadFromJsonAsync<IEnumerable<GetEmployeeResponse>>();
        Assert.NotNull(employees);
        Assert.Single(employees);
    }

    [Fact]
    public async Task GetEmployeeById_ReturnsOkResult()
    {
        HttpClient client = _factory.CreateClient();
        var response = await client.GetAsync("/employees/1");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateEmployee_ReturnsCreatedResult()
    {
        HttpClient client = await _factory.CreateAuthenticatedClient("createemployee@test.com");

        var response = await client.PostAsJsonAsync("/employees", new Employee
        {
            FirstName = "Alice", // Changed from "John"
            LastName = "Johnson",
            SocialSecurityNumber = "123-45-3445"
        });

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateEmployee_ReturnsBadRequestResult()
    {
        // Arrange
        HttpClient client = await _factory.CreateAuthenticatedClient("createemptyemployee@test.com");
        // Empty object to trigger validation errors
        var invalidEmployee = new CreateEmployeeRequest();

        // Act
        var response = await client.PostAsJsonAsync("/employees", invalidEmployee);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Contains("FirstName", problemDetails.Errors.Keys);
        Assert.Contains("LastName", problemDetails.Errors.Keys);
        Assert.Contains("First name is required.", problemDetails.Errors["FirstName"]);
        Assert.Contains("Last name is required.", problemDetails.Errors["LastName"]);

    }

    [Fact]
    public async Task UpdateEmployee_ReturnsOkResults()
    {
        HttpClient client = await _factory.CreateAuthenticatedClient("updateemployee@test.com");

        var response = await client.PutAsJsonAsync("/employees/1", new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            Address1 = "123 Main Smoot"
        });

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Failed to update employee: {content}");
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var employee = await db.Employees.FindAsync(1);
        Assert.Equal("123 Main Smoot", employee?.Address1);
        Assert.Equal(CustomWebApplicationFactory.SystemClock.UtcNow.UtcDateTime, employee!.LastModifiedOn);

    }

    [Fact]
    public async Task UpdateEmployee_ReturnsNotFoundForNonExistentEmployee()
    {
        HttpClient client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync("/employees/99999", new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            SocialSecurityNumber = "123-45-3445",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEmployee_ReturnsBadRequestWhenAddress()
    {
        // Arrange
        HttpClient client = await _factory.CreateAuthenticatedClient();

        var invalidEmployee = new UpdateEmployeeRequest(); // Empty object to trigger validation errors

        // Act
        var response = await client.PutAsJsonAsync($"/employees/{_employeeId}", invalidEmployee);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Contains("Address1", problemDetails.Errors.Keys);
    }

    [Fact]
    public async Task UpdateEmployee_LogsAndReturnsOkResult()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EmployeesController>>().SetupAllProperties();

        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_UpdateEmployee")
            .Options;

        // Create a mock ISystemClock for testing
        var mockSystemClock = new Mock<ISystemClock>();
        mockSystemClock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        // Pass both required parameters to AppDbContext constructor
        using var context = new AppDbContext(options, mockSystemClock.Object);

        var employeeId = 1;
        // Seed the employee
        context.Employees.Add(new Employee
        {
            Id = employeeId,
            FirstName = "Test",
            LastName = "Mock"
        });
        context.SaveChanges();

        var controller = new EmployeesController(loggerMock.Object, context);

        var updateRequest = new UpdateEmployeeRequest { City = "East Jarod", State = "Maryland", };

        // Act
        var result = await controller.UpdateEmployee(employeeId, updateRequest);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,                        // Level
                It.IsAny<EventId>(),                         // EventId
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains($"Updating employee with ID: {employeeId}")),
                It.IsAny<Exception>(),                       // Exception
                It.IsAny<Func<It.IsAnyType, Exception?, string>>() // Formatter
            ),
            Times.Once // Or Times.AtLeastOnce()
        );
    }
    
    [Fact]
    public async Task DeleteEmployee_ReturnsNoContentResult()
    {
        HttpClient client = await _factory.CreateAuthenticatedClient();

        var newEmployee = new Employee { FirstName = "Meow", LastName = "Garita" };
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Employees.Add(newEmployee);
            await db.SaveChangesAsync();
        }

        var response = await client.DeleteAsync($"/employees/{newEmployee.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEmployee_ReturnsNotFoundResult()
    {
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/employees/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
        
    [Fact]
    public async Task GetBenefitsForEmployee_ReturnsOkResult()
    {
        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/employees/{_employeeId}/benefits");

        // Assert
        response.EnsureSuccessStatusCode();

        var benefits = await response.Content.ReadFromJsonAsync<IEnumerable<GetEmployeeResponseEmployeeBenefit>>();
        Assert.NotNull(benefits);
        // John has two benefits.
        Assert.Equal(2, benefits.Count());
    }
}