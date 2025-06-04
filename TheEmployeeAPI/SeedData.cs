using System;
using Microsoft.EntityFrameworkCore;

namespace TheEmployeeAPI;

public static class SeedData
{
  public static void MigrateAndSeed(IServiceProvider serviceProvider)
  {
    var context = serviceProvider.GetRequiredService<AppDbContext>();
    // Ensure the database is created and all migrations are applied before seeding data.
    // This is necessary for integration tests because the test runner may use a fresh or in-memory database
    // that does not exist or is not up-to-date with the latest schema. Without this call, attempts to
    // insert or query data could fail due to missing tables or columns.
    context.Database.Migrate();

    if (!context.Employees.Any())
    {
      context.Employees.AddRange(
        new Employee
        {
          FirstName = "John",
          LastName = "Doe",
          SocialSecurityNumber = "123-45-6789",
          Address1 = "123 Main St",
          City = "Anytown",
          State = "NY",
          ZipCode = "12345",
          PhoneNumber = "555-123-4567",
          Email = "john.doe@example.com"
        },
        new Employee
        {
          FirstName = "Jane",
          LastName = "Smith",
          SocialSecurityNumber = "987-65-4321",
          Address1 = "456 Elm St",
          Address2 = "Apt 2B",
          City = "Othertown",
          State = "CA",
          ZipCode = "98765",
          PhoneNumber = "555-987-6543",
          Email = "jane.smith@example.com"
        }
      );

      context.SaveChanges();
    }
  }
}
