using System.Net.Http.Json;
using TheEmployeeAPI.Users;

namespace TheEmployeeAPI.Tests
{
    
  public static class TestHelpers
  {
    public static async Task<HttpClient> CreateAuthenticatedClient(
      // Extension method, allow 
      this CustomWebApplicationFactory factory,
      string email = "test@test.com"
      )
    {
        var client = factory.CreateClient();

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
}