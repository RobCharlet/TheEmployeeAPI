using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TheEmployeeAPI.Users;

namespace TheEmployeeAPI.Tests
{
    
  public static class TestHelpers
  {
    public static async Task<HttpClient> CreateAuthenticatedClient(
      // Extension method, allow 
      this CustomWebApplicationFactory factory,
      string email = "test@test.com",
      bool allowAutoRedirect = true
      )
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowAutoRedirect
        });

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

    public static async Task<FormUrlEncodedContent> CreateFormDataWithToken(
      HttpClient client, 
      string tokenUrl, 
      Dictionary<string, string> formFields
    )
    {
        var token = await GetAntiForgeryTokenFromPage(client, tokenUrl);
        formFields["__RequestVerificationToken"] = token;
        return new FormUrlEncodedContent(formFields);
    }

    private static async Task<string> GetAntiForgeryTokenFromPage(HttpClient client, string url)
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
  }
}