using System.Net;

namespace TheEmployeeAPI.Tests;

// Forces all tests in this collection to run sequentially instead of in parallel
// This prevents conflicts when sharing resources like database containers, web servers, and network ports
[Collection("Sequential")]
public class AccountControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AccountControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AccountLogin_WithValidCredentials_RedirectsToEmployeesIndex()
    {
        var client = _factory.CreateClient();

        var formData = await TestHelpers.CreateFormDataWithToken(client, "/account/login", new Dictionary<string, string>
        {
            {"Email", "admin@admin.com"},
            {"Password", "Admin123!"},
            {"RememberMe", "false"}
        });

        var response = await client.PostAsync("/account/login", formData);

        response.EnsureSuccessStatusCode();
        
        // Check that we are redirected to employees page
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Employés", content); // Should contain the employees page content
    }

    [Fact]
    public async Task AccountLogin_WithInvalidData_ReturnsViewWithValidationErrors()
    {
        var client = _factory.CreateClient();

        var formData = await TestHelpers.CreateFormDataWithToken(client, "/account/login", new Dictionary<string, string>
        {
            {"Email", ""},
            {"Password", ""},
            {"RememberMe", "false"}
        });

        var response = await client.PostAsync("/account/login", formData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Email is required", content);
        Assert.Contains("Password is required", content);
    }

    [Fact]
    public async Task AccountLogout_RedirectsToLogin()
    {
        var client = await _factory.CreateAuthenticatedClient("logouttest@test.com", false);

        var formData = await TestHelpers.CreateFormDataWithToken(client, "/employees", new Dictionary<string, string>());
        var response = await client.PostAsync("/account/logout", formData);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/login", response.Headers.Location?.ToString());
    }
}