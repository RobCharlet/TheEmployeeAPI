using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using TheEmployeeAPI.Users;

namespace TheEmployeeAPI.Tests;

public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Unauthorized tests
    [Fact]
    public async Task GetAllUsers_WithoutAuthentication_Returns401() 
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users/test-id");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users/current");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginUser_WithInvalidCredentials_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var request = new LoginRequest
        {
            Email = "nonexistent@test.com",
            Password = "WrongPassword123!",
            RememberMe = false
        };

        var response = await client.PostAsJsonAsync("/api/users/login", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Authorized tests
    [Fact]
    public async Task RegisterUser_ReturnsCreatedResult()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/register", new RegisterRequest
        {
            Email = "newuser@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            FirstName = "Test",
            LastName = "User"
        });

        response.EnsureSuccessStatusCode();
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.True(authResponse.IsAuthenticated);
        Assert.Equal("newuser@test.com", authResponse.Email);
    }

    [Fact]
    public async Task LoginUser_WithValidCredentials_ReturnsOkResult()
    {
        var client = await CreateAuthenticatedClient();

        var request = new LoginRequest
        {
            Email = "test@test.com", 
            Password = "Test123!",
            RememberMe = false
        };

        var response = await client.PostAsJsonAsync("/api/users/login", request);

        response.EnsureSuccessStatusCode();
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.True(authResponse.IsAuthenticated);
        Assert.Equal("test@test.com", authResponse.Email);
    }

    [Fact]
    public async Task GetAllUsers_WithAuthentication_ReturnsOkResult()
    {
        var client = await CreateAuthenticatedClient();

        // Try immediate request with same client
        var response = await client.GetAsync("/api/users");        
        
        response.EnsureSuccessStatusCode();
        var users = await response.Content.ReadFromJsonAsync<IEnumerable<GetUserResponse>>();
        Assert.NotNull(users);
    }

    [Fact]
    public async Task UpdateProfile_WithAuthentication_ReturnsOkResult()
    {
        var client = await CreateAuthenticatedClient();
        
        // Get user ID via current user endpoint
        var currentUserResponse = await client.GetAsync("/api/users/current");
        var user = await currentUserResponse.Content.ReadFromJsonAsync<GetUserResponse>();

        var updateRequest = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "Name",
            ProfilePicture = "https://example.com/avatar.jpg"
        };

        var response = await client.PutAsJsonAsync($"/api/users/{user!.Id}", updateRequest);

        response.EnsureSuccessStatusCode();
        var updatedUser = await response.Content.ReadFromJsonAsync<GetUserResponse>();
        Assert.NotNull(updatedUser);
        Assert.Equal("Updated", updatedUser.FirstName);
        Assert.Equal("https://example.com/avatar.jpg", updatedUser.ProfilePicture);
    }

    [Fact]
    public async Task Logout_WithAuthentication_ReturnsOkResult()
    {
        var client = await CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/users/logout", new { });

        response.EnsureSuccessStatusCode();
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.False(authResponse.IsAuthenticated);
        Assert.Equal("Logout successful", authResponse.Message);
    }

    [Fact]
    public async Task RegisterUser_WithInvalidData_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var invalidRequest = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "weak",
            ConfirmPassword = "different",
            FirstName = "",
            LastName = ""
        };

        var response = await client.PostAsJsonAsync("/api/users/register", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.True(problemDetails.Errors.Count > 0);
    }

    [Fact]
    public async Task DeactivateUser_ReturnsOkResult() {
        var client = await CreateAuthenticatedClient();

        // Get user ID via current user endpoint
        var currentUserResponse = await client.GetAsync("/api/users/current");
        var user = await currentUserResponse.Content.ReadFromJsonAsync<GetUserResponse>();

        var response = await client.DeleteAsync($"/api/users/{user!.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var deactivatedResponse = await client.GetAsync($"/api/users/{user!.Id}");
        var deactivatedUser = await deactivatedResponse.Content.ReadFromJsonAsync<GetUserResponse>();
        Assert.False(deactivatedUser!.IsActive);
    }

    // Utils
    private async Task<HttpClient> CreateAuthenticatedClient(string email = "test@test.com")
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/users/register", new RegisterRequest
        {
            Email = email,
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            FirstName = "Test",
            LastName = "User"
        });

        return client;
    }
}