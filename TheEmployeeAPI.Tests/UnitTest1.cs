using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TheEmployeeAPI.Abstractions;

namespace TheEmployeeAPI.Tests;

public class BasicTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BasicTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;

        var repo = _factory.Services.GetRequiredService<IRepository<Employee>>();
        repo.Create(new Employee { FirstName = "John", LastName = "Doe" });
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
        Assert.Contains("The FirstName field is required.", problemDetails.Errors["FirstName"]);
        Assert.Contains("The LastName field is required.", problemDetails.Errors["LastName"]);

    }

    [Fact]
    public async Task UpdateEmployee_ReturnsOkResults()
    {
        HttpClient client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync("/employees/1", new Employee
        {
            FirstName = "John",
            LastName = "Doe"
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
            SocialSecurityNumber = "123-45-3445"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}