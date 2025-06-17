using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TheEmployeeAPI.Users;

namespace TheEmployeeAPI.Tests;

// Forces all tests in this collection to run sequentially instead of in parallel
// This prevents conflicts when sharing resources like database containers, web servers, and network ports
[Collection("Sequential")]
public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<User> CreateTestUser(string email = "test@example.com", string firstName = "Test", string lastName = "User")
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<User>>();
        
        var user = new User
        {
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true
        };
        
        await userManager.CreateAsync(user, "Test123!");
        return user;
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
        var client = await _factory.CreateAuthenticatedClient("login@test.com");

        // Logout automatically connected new user
        await client.PostAsJsonAsync("/api/users/logout", new {});

        var request = new LoginRequest
        {
            Email = "login@test.com",
            Password = "Test123!",
            RememberMe = false
        };

        var response = await client.PostAsJsonAsync("/api/users/login", request);

        response.EnsureSuccessStatusCode();
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.True(authResponse.IsAuthenticated);
        Assert.Equal("login@test.com", authResponse.Email);
    }

    [Fact]
    public async Task GetAllUsers_WithAuthentication_ReturnsOkResult()
    {
        var client = await _factory.CreateAuthenticatedClient("getallusers@test.com");

        var response = await client.GetAsync("/api/users");        
        
        response.EnsureSuccessStatusCode();
        var users = await response.Content.ReadFromJsonAsync<IEnumerable<GetUserResponse>>();
        Assert.NotNull(users);
    }

    [Fact]
    public async Task UpdateProfile_WithAuthentication_ReturnsOkResult()
    {
        var client = await _factory.CreateAuthenticatedClient("updateprofile@test.com");
        
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
        var client = await _factory.CreateAuthenticatedClient("logout@test.com");

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
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.Errors);
        Assert.True(errorResponse.Errors.Any());
        Assert.Contains("Passwords must be at least 8 characters.", errorResponse.Errors);
        Assert.Contains("Passwords must have at least one uppercase ('A'-'Z').", errorResponse.Errors);
    }

    [Fact]
    public async Task DeactivateUser_ReturnsOkResult() 
    {
        var client = await _factory.CreateAuthenticatedClient();

        // Get user ID via current user endpoint
        var currentUserResponse = await client.GetAsync("/api/users/current");
        var user = await currentUserResponse.Content.ReadFromJsonAsync<GetUserResponse>();

        var response = await client.DeleteAsync($"/api/users/{user!.Id}");
        response.EnsureSuccessStatusCode();

        var deactivatedResponse = await client.GetAsync($"/api/users/{user!.Id}");
        var deactivatedUser = await deactivatedResponse.Content.ReadFromJsonAsync<GetUserResponse>();
        Assert.NotNull(deactivatedUser);
        Assert.False(deactivatedUser.IsActive);
    }

    // Password tests
    [Fact]
    public async Task ChangePassword_WithValidData_ReturnsOkResult()
    {
        var client = await _factory.CreateAuthenticatedClient("changepass@test.com");

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "Test123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        var response = await client.PostAsJsonAsync("/api/users/change-password", request);

        response.EnsureSuccessStatusCode();
        var passwordResponse = await response.Content.ReadFromJsonAsync<PasswordResponse>();
        Assert.NotNull(passwordResponse);
        Assert.True(passwordResponse.Success);
        Assert.Equal("Password changed successfully", passwordResponse.Message);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "Test123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        var response = await client.PostAsJsonAsync("/api/users/change-password", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithIncorrectCurrentPassword_ReturnsBadRequest()
    {
        var client = await _factory.CreateAuthenticatedClient("changepassfail@test.com");

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        var response = await client.PostAsJsonAsync("/api/users/change-password", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var passwordResponse = await response.Content.ReadFromJsonAsync<PasswordResponse>();
        Assert.NotNull(passwordResponse);
        Assert.False(passwordResponse.Success);
        Assert.Equal("Password change failed", passwordResponse.Message);
    }

    [Fact]
    public async Task ForgotPassword_WithValidEmail_ReturnsOkResult()
    {
        var client = _factory.CreateClient();
        await CreateTestUser("forgot@test.com");

        var request = new ForgotPasswordRequest
        {
            Email = "forgot@test.com"
        };

        var response = await client.PostAsJsonAsync("/api/users/forgot-password", request);

        response.EnsureSuccessStatusCode();
        var passwordResponse = await response.Content.ReadFromJsonAsync<PasswordResponse>();
        Assert.NotNull(passwordResponse);
        Assert.True(passwordResponse.Success);
        Assert.Equal("A password reset link has been sent.", passwordResponse.Message);
    }

    [Fact]
    public async Task ForgotPassword_WithNonExistentEmail_ReturnsOkResult()
    {
        var client = _factory.CreateClient();

        var request = new ForgotPasswordRequest
        {
            Email = "nonexistent@test.com"
        };

        var response = await client.PostAsJsonAsync("/api/users/forgot-password", request);

        // Should return OK for security reasons even with non-existent email
        response.EnsureSuccessStatusCode();
        var passwordResponse = await response.Content.ReadFromJsonAsync<PasswordResponse>();
        Assert.NotNull(passwordResponse);
        Assert.True(passwordResponse.Success);
        Assert.Equal("A password reset link has been sent.", passwordResponse.Message);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ReturnsOkResult()
    {
        var client = _factory.CreateClient();
        
        // Register user through API to ensure proper setup
        await client.PostAsJsonAsync("/api/users/register", new RegisterRequest
        {
            Email = "reset@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            FirstName = "Test",
            LastName = "User"
        });

        // Get the user from database to generate a real token
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<User>>();
        var user = await userManager.FindByEmailAsync("reset@test.com");
        var token = await userManager.GeneratePasswordResetTokenAsync(user!);

        var request = new ResetPasswordRequest
        {
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        // Use proper URL encoding for the token
        var encodedToken = Uri.EscapeDataString(token);
        var response = await client.PostAsJsonAsync($"/api/users/reset-password?email=reset@test.com&token={encodedToken}", request);

        response.EnsureSuccessStatusCode();
        var passwordResponse = await response.Content.ReadFromJsonAsync<PasswordResponse>();
        Assert.NotNull(passwordResponse);
        Assert.True(passwordResponse.Success);
        Assert.Equal("Password reset successfully", passwordResponse.Message);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var request = new ResetPasswordRequest
        {
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        var response = await client.PostAsJsonAsync("/api/users/reset-password?email=test@test.com&token=invalid-token", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var passwordResponse = await response.Content.ReadFromJsonAsync<PasswordResponse>();
        Assert.NotNull(passwordResponse);
        Assert.False(passwordResponse.Success);
        Assert.Equal("Invalid request", passwordResponse.Message);
    }

    [Fact]
    public async Task ResetPassword_WithNonExistentEmail_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var request = new ResetPasswordRequest
        {
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        var response = await client.PostAsJsonAsync("/api/users/reset-password?email=nonexistent@test.com&token=some-token", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var passwordResponse = await response.Content.ReadFromJsonAsync<PasswordResponse>();
        Assert.NotNull(passwordResponse);
        Assert.False(passwordResponse.Success);
        Assert.Equal("Invalid request", passwordResponse.Message);
    }
}