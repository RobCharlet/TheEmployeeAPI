using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using TheEmployeeAPI;

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
            // Remove production database
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(dbContextDescriptor);

            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbConnection));
            services.Remove(dbConnectionDescriptor);

            // Remove production ISystemClock and replace with test version
            var systemClockDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ISystemClock));
            if (systemClockDescriptor != null)
            {
                services.Remove(systemClockDescriptor);
            }

            // Register our test SystemClock with fixed time
            services.AddSingleton<ISystemClock>(SystemClock);

            // Create in-memory database for tests
            services.AddSingleton<DbConnection>(container =>
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();
                return connection;
            });

            services.AddDbContext<AppDbContext>((container, options) =>
            {
                var connection = container.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
                // Enable tracking for tests to see audit field updates
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
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