using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Fixed time for all tests - makes tests predictable and reliable
    // Without this: DateTime.UtcNow changes every millisecond → tests fail randomly
    // With this: time is always 2022-01-01 → tests can verify exact values
    public static TestSystemClock SystemClock { get; } = new TestSystemClock();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var systemClockDescriptor = services.Single(d => d.ServiceType == typeof(ISystemClock));
            services.Remove(systemClockDescriptor);
            services.AddSingleton<ISystemClock>(SystemClock);

            // Modify cookie policy (allow http) for tests purposes
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
                //options.Cookie.SameSite = SameSiteMode.Lax;
            });
        });
    }

    // Test implementation that always returns the same time
    // This makes audit field tests predictable: we know exactly what time will be set
    public class TestSystemClock : ISystemClock
    {
        // Fixed time for all tests - never changes
        public DateTimeOffset UtcNow { get; } = DateTimeOffset.Parse("2022-01-01T00:00:00Z");
    }
}