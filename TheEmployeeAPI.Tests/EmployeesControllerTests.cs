using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TheEmployeeAPI.Tests;

// Forces all tests in this collection to run sequentially instead of in parallel
// This prevents conflicts when sharing resources like database containers, web servers, and network ports
[Collection("Sequential")]
public class EmployeesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly int _employeeId = 1;
    private readonly CustomWebApplicationFactory _factory;

    public EmployeesControllerTests(
        CustomWebApplicationFactory factory
    )
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

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("John", content);
    }

    [Fact]
    public async Task GetEmployeeById_ReturnsOkResult()
    {
        HttpClient client = _factory.CreateClient();
        var response = await client.GetAsync("/employees/details/1");

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("John", content);
    }

    [Fact]
    public async Task CreateEmployee_ReturnsCreatedResult()
    {
        HttpClient client = await _factory.CreateAuthenticatedClient("createemployee@test.com");

        var response = await client.PostAsJsonAsync("/employees", new Employee
        {
            FirstName = "Alice",
            LastName = "Johnson",
            SocialSecurityNumber = "123-45-3445"
        });

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateEmployee_WithEmptyFields_ReturnsViewWithValidationErrors()
    {
        // Arrange
        HttpClient client = await _factory.CreateAuthenticatedClient("createemptyemployee@test.com");
        
        // Get the create form first to get antiforgery token
        var getResponse = await client.GetAsync("/employees/create");
        getResponse.EnsureSuccessStatusCode();
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract antiforgery token
        var tokenMatch = System.Text.RegularExpressions.Regex.Match(
            getContent, 
            @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)"""
        );
        var token = tokenMatch.Success ? tokenMatch.Groups[1].Value : "";
        
        // Send form with empty required fields
        var formData = new Dictionary<string, string>
        {
            {"__RequestVerificationToken", token},
            {"FirstName", ""},
            {"LastName", ""}
        };
        
        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await client.PostAsync("/employees/create", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var decodedContent = System.Net.WebUtility.HtmlDecode(content);
        
        // Verify that we are on the Create page and not redirected to Index
        Assert.Contains("Ajouter un employé", decodedContent);
        
        // Verify that we are on the Create page again and validation errors are shown
        Assert.Contains("Le prénom est obligatoire.", decodedContent);
        Assert.Contains("Le nom est obligatoire.", decodedContent);
        
        // Verify no employee was actually created with empty fields
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var employees = await db.Employees.ToListAsync();
        
        // Should only have seed data (John and Jane)
        Assert.Equal(2, employees.Count);
    }

    private async Task<string> GetAntiForgeryTokenFromPage(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        var tokenMatch = System.Text.RegularExpressions.Regex.Match(
            content, 
            @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)"""
        );
        
        return tokenMatch.Success ? tokenMatch.Groups[1].Value : "";
    }

    private async Task<Employee> CreateTestEmployee(string firstName = "Test", string lastName = "Employee")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var employee = new Employee
        {
            FirstName = firstName,
            LastName = lastName,
            SocialSecurityNumber = $"{Random.Shared.Next(100, 999)}-{Random.Shared.Next(10, 99)}-{Random.Shared.Next(1000, 9999)}",
            Address1 = "15 rue de la Paix",
            City = "Paris",
            State = "Île-de-France",
            ZipCode = "75001",
            PhoneNumber = "01 23 45 67 89",
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}@test.com"
        };
        
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        return employee;
    }

    private async Task<FormUrlEncodedContent> CreateFormDataWithToken(HttpClient client, string tokenUrl, Dictionary<string, string> formFields)
    {
        var token = await GetAntiForgeryTokenFromPage(client, tokenUrl);
        formFields["__RequestVerificationToken"] = token;
        return new FormUrlEncodedContent(formFields);
    }

    [Fact]
    public async Task UpdateEmployee_ReturnsNotFoundForNonExistentEmployee()
    {
        HttpClient client = await _factory.CreateAuthenticatedClient("updatenotfound@test.com");

        var response = await client.GetAsync("/employees/edit/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEmployee_WithInvalidData_ReturnsViewWithValidationErrors()
    {
        // Arrange
        HttpClient client = await _factory.CreateAuthenticatedClient("updateinvalid@test.com");

        // Act
        var formData = await CreateFormDataWithToken(client, $"/employees/edit/{_employeeId}", new Dictionary<string, string>
        {
            {"Address1", ""}, // This is required
            {"City", "Lyon"},
            {"State", "Rhône-Alpes"},
            {"ZipCode", "69000"}
        });
        
        var response = await client.PostAsync($"/employees/edit/{_employeeId}", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var decodedContent = System.Net.WebUtility.HtmlDecode(content);

        Assert.Contains("Modifier l'employé", decodedContent);
        Assert.Contains("Address1 must not be empty as an address was already set on the employee.", decodedContent);
    }

    [Fact]
    public async Task UpdateEmployee_ReturnsOkResults()
    {
        // Arrange
        HttpClient client = await _factory.CreateAuthenticatedClient("updateemployee@test.com");
        var testEmployee = await CreateTestEmployee();

        // Act
        var formData = await CreateFormDataWithToken(client, $"/employees/edit/{testEmployee.Id}", new Dictionary<string, string>
        {
            {"Address1", "42 avenue des Champs-Élysées"},
            {"Address2", "Appartement 3B"},
            {"City", "Marseille"},
            {"State", "Provence-Alpes-Côte d'Azur"},
            {"ZipCode", "13001"},
            {"PhoneNumber", "06 12 34 56 78"},
            {"Email", "updated@example.com"}
        });
        
        var response = await client.PostAsync($"/employees/edit/{testEmployee.Id}", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var decodedContent = System.Net.WebUtility.HtmlDecode(content);
        
        Assert.Contains("Employé modifié avec succès !", decodedContent);
        Assert.Contains("Détails de l'employé", decodedContent);
        
        // Verify the employee was actually updated in the database
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updatedEmployee = await verifyDb.Employees
            .AsTracking()
            .Include(e => e.EmployeeBenefits)
            .SingleOrDefaultAsync(e => e.Id == testEmployee.Id);
        
        Assert.NotNull(updatedEmployee);
        Assert.Equal("42 avenue des Champs-Élysées", updatedEmployee.Address1);
        Assert.Equal("Appartement 3B", updatedEmployee.Address2);
        Assert.Equal("Marseille", updatedEmployee.City);
        Assert.Equal("Provence-Alpes-Côte d'Azur", updatedEmployee.State);
        Assert.Equal("13001", updatedEmployee.ZipCode);
        Assert.Equal("06 12 34 56 78", updatedEmployee.PhoneNumber);
        Assert.Equal("updated@example.com", updatedEmployee.Email);
    }

    [Fact]
    public async Task DeleteEmployee_RedirectsToIndex()
    {
        HttpClient client = await _factory.CreateAuthenticatedClient("deleteemployee@test.com");
        var newEmployee = await CreateTestEmployee("Jean", "Claude");

        // Act
        var formData = await CreateFormDataWithToken(client, "/employees", new Dictionary<string, string>());
        var response = await client.PostAsync($"/employees/delete/{newEmployee.Id}", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify the employee was actually deleted
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deletedEmployee = await db.Employees.FindAsync(newEmployee.Id);
        Assert.Null(deletedEmployee);
    }

    [Fact]
    public async Task DeleteEmployee_ReturnsNotFoundResult()
    {
        var client = await _factory.CreateAuthenticatedClient("deletenotfound@test.com");
        var response = await client.DeleteAsync("/employees/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}