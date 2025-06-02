using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TheEmployeeAPI.Abstractions;
using TheEmployeeAPI.Employees;

namespace TheEmployeeAPI.Tests;

public class BasicTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly int _employeeId = 1;
    private readonly WebApplicationFactory<Program> _factory;

    public BasicTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;

        var repo = _factory.Services.GetRequiredService<IRepository<Employee>>();
        repo.Create(new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            Address1 = "123 Main St",
            Benefits = new List<EmployeeBenefits>
            {
                new EmployeeBenefits { BenefitType = BenefitType.Health, Cost = 100 },
                new EmployeeBenefits { BenefitType = BenefitType.Dental, Cost = 50 }
            }
        });
        _employeeId = repo.GetAll().First().Id;
    }

    [Fact]
    public async Task GetAllEmployees_ReturnsOkResult()
    {
        HttpClient client = _factory.CreateClient();
        var response = await client.GetAsync("/employees");

        response.EnsureSuccessStatusCode();
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
        HttpClient client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/employees", new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            SocialSecurityNumber = "123-45-3445"
        });

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateEmployee_ReturnsBadRequestResult()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
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
        HttpClient client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync("/employees/1", new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            Address1 = "123 Main St"
        });

        response.EnsureSuccessStatusCode();
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
        var client = _factory.CreateClient();
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
        var repositoryMock = new Mock<IRepository<Employee>>();
        var controller = new EmployeesController(repositoryMock.Object, loggerMock.Object);

        var employeeId = 1;
        var updateRequest = new UpdateEmployeeRequest { City = "East Jarod", State = "Maryland", };

        repositoryMock.Setup(r => r.GetById(employeeId)).Returns(new Employee { Id = employeeId, FirstName = "Test", LastName = "Mock" });

        // Act
        var result = await Task.Run(() => controller.UpdateEmployee(employeeId, updateRequest));

        // Assert
        Assert.IsType<OkObjectResult>(result);

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